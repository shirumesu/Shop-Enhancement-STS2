using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ShopEnhancement.Compatibility;
using ShopEnhancement.Patches;

namespace ShopEnhancement.Network;

/// <summary>
/// Replicates the mod's shop actions in multiplayer. The acting client broadcasts what it did, then every client
/// (including the actor) executes the same action for that player, so all copies of the run state stay identical:
/// <list type="bullet">
/// <item><see cref="ShopServiceMessage"/>: enchant / gift-card service bought from the card-removal slot.</item>
/// <item><see cref="ShopRefreshMessage"/>: shop refresh (replayed, so shop RNG and relic pool advance everywhere).</item>
/// <item><see cref="ShopGiftMessage"/>: gift-mode purchase (sender pays, target receives).</item>
/// <item><see cref="ShopSellMessage"/>: selling a relic / potion.</item>
/// </list>
/// Messages go through the run's location buffer, so they are applied in order and only once the receiver is at the
/// same map location. One instance lives next to each <see cref="OneOffSynchronizer"/>.
/// </summary>
internal sealed class ShopActionSynchronizer : IDisposable
{
	internal static readonly ConditionalWeakTable<OneOffSynchronizer, ShopActionSynchronizer> Instances = new();

	private readonly RunLocationTargetedMessageBuffer _messageBuffer;

	private readonly INetGameService _netService;

	private readonly IPlayerCollection _players;

	internal ShopActionSynchronizer(RunLocationTargetedMessageBuffer messageBuffer, INetGameService netService, IPlayerCollection players)
	{
		_messageBuffer = messageBuffer;
		_netService = netService;
		_players = players;
		_messageBuffer.RegisterMessageHandler<ShopServiceMessage>(HandleService);
		_messageBuffer.RegisterMessageHandler<ShopRefreshMessage>(HandleRefresh);
		_messageBuffer.RegisterMessageHandler<ShopGiftMessage>(HandleGift);
		_messageBuffer.RegisterMessageHandler<ShopSellMessage>(HandleSell);
	}

	public void Dispose()
	{
		_messageBuffer.UnregisterMessageHandler<ShopServiceMessage>(HandleService);
		_messageBuffer.UnregisterMessageHandler<ShopRefreshMessage>(HandleRefresh);
		_messageBuffer.UnregisterMessageHandler<ShopGiftMessage>(HandleGift);
		_messageBuffer.UnregisterMessageHandler<ShopSellMessage>(HandleSell);
	}

	/// <summary>The synchronizer of the current run.</summary>
	internal static ShopActionSynchronizer Current
	{
		get
		{
			if (!Instances.TryGetValue(RunManager.Instance.OneOffSynchronizer, out var synchronizer))
			{
				throw new InvalidOperationException("Shop action synchronizer has not been initialized.");
			}
			return synchronizer;
		}
	}

	private Player LocalPlayer => GetPlayer(_netService.NetId);

	private Player GetPlayer(ulong id)
	{
		return _players.GetPlayer(id) ?? throw new InvalidOperationException($"Shop service player {id} is not in the run.");
	}

	internal Task<bool> DoLocalService(ShopServiceMessage message)
	{
		message.Location = _messageBuffer.CurrentLocation;
		_netService.SendMessage(message);
		return ExecuteService(LocalPlayer, message);
	}

	private void HandleService(ShopServiceMessage message, ulong senderId)
	{
		TaskHelper.RunSafely(ExecuteService(GetPlayer(senderId), message));
	}

	private async Task<bool> ExecuteService(Player player, ShopServiceMessage message)
	{
		MerchantCardRemovalEntry entry = GameCompat.GetInventoryFor(player).CardRemovalEntry ?? throw new InvalidOperationException("Shop service entry is missing.");
		EnchantmentModel? enchantment = message.IsGift ? null : MerchantCardRemovalPatches.ResolveEnchantment(message.EnchantmentIndex);
		LocString prompt = CardSelectorPrefs.EnchantSelectionPrompt;
		if (message.IsGift)
		{
			prompt = new LocString("shop_enhancement", "gift.service_select_prompt");
			prompt.Add("0", ShopEnhancementNetwork.DescribePlayer(GetPlayer(message.TargetId)));
		}
		var prefs = new CardSelectorPrefs(prompt, message.CardCount)
		{
			Cancelable = message.Cancelable,
			RequireManualConfirmation = true
		};
		List<CardModel> selectedCards = (await CardSelectCmd.FromDeckGeneric(player, prefs, cardModel =>
		{
			EnchantmentModel? enchantmentModel = enchantment;
			if (enchantmentModel == null)
			{
				CardPile? pile = cardModel.Pile;
				if (pile == null)
				{
					return false;
				}
				return pile.Type == PileType.Deck;
			}
			return enchantmentModel.CanEnchant(cardModel);
		})).ToList();
		if (selectedCards.Count == 0)
		{
			return false;
		}
		if (message.GoldCost > 0)
		{
			await PlayerCmd.LoseGold(message.GoldCost, player, GoldLossType.Spent);
		}
		if (message.IsGift)
		{
			Player target = GetPlayer(message.TargetId);
			foreach (CardModel card in selectedCards)
			{
				CardPileAddResult result = await CardPileCmd.Add(target.RunState.LoadCard(card.ToSerializable(), target), PileType.Deck);
				if (!result.success)
				{
					throw new InvalidOperationException($"Could not give {card.Id} to player {target.NetId}.");
				}
				await CardPileCmd.RemoveFromDeck(card);
				if (!target.DiscoveredCards.Contains(result.cardAdded.Id))
				{
					target.DiscoveredCards.Add(result.cardAdded.Id);
				}
				if (LocalContext.IsMe(target))
				{
					CardCmd.PreviewCardPileAdd(result);
				}
			}
			if (LocalContext.IsMe(target))
			{
				ShopEnhancementNetwork.ShowGiftNotification(target, player, "gift_received_bubble");
			}
		}
		else
		{
			CardModel enchantedCard = selectedCards.Single();
			CardCmd.Enchant(enchantment!, enchantedCard, message.EnchantmentAmount);
			if (LocalContext.IsMe(player))
			{
				NCardEnchantVfx? enchantVfx = NCardEnchantVfx.Create(enchantedCard);
				if (enchantVfx != null)
				{
					NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(enchantVfx);
				}
			}
		}
		await Hook.AfterItemPurchased(player.RunState, player, entry, message.GoldCost);
		return true;
	}

	// ---- Shop refresh ----

	/// <summary>Refreshes the local player's shop everywhere. Returns the new inventory.</summary>
	internal Task<MerchantInventory> DoLocalRefresh(int goldCost)
	{
		var message = new ShopRefreshMessage
		{
			Location = _messageBuffer.CurrentLocation,
			GoldCost = goldCost
		};
		_netService.SendMessage(message);
		return ExecuteRefresh(LocalPlayer, message);
	}

	private void HandleRefresh(ShopRefreshMessage message, ulong senderId)
	{
		TaskHelper.RunSafely(ExecuteRefresh(GetPlayer(senderId), message));
	}

	private static async Task<MerchantInventory> ExecuteRefresh(Player player, ShopRefreshMessage message)
	{
		// The inventory swap runs synchronously, before the first await, so a purchase message the same player sends
		// right after the refresh is applied to the new inventory.
		MerchantInventory newInventory = ShopRefreshPatches.RebuildInventory(player);
		await PlayerCmd.LoseGold(message.GoldCost, player, GoldLossType.Spent);
		return newInventory;
	}

	// ---- Gift-mode purchase ----

	private static readonly MethodInfo ClearAfterPurchaseMethod = AccessTools.Method(typeof(MerchantEntry), "ClearAfterPurchase");

	private static readonly MethodInfo RestockAfterPurchaseMethod = AccessTools.Method(typeof(MerchantEntry), "RestockAfterPurchase");

	/// <summary>Shop entries that can be gifted, in a stable order shared by all clients.</summary>
	internal static List<MerchantEntry> GetGiftableEntries(MerchantInventory inventory)
	{
		return inventory.CardEntries.Cast<MerchantEntry>().Concat(inventory.RelicEntries).Concat(inventory.PotionEntries).ToList();
	}

	/// <summary>
	/// Whether <paramref name="target"/> can take <paramref name="potion"/>: the same checks
	/// <see cref="PotionCmd.TryToProcure"/> makes (e.g. Sozu forbids it, or every potion slot is taken).
	/// </summary>
	internal static PurchaseStatus CheckCanReceivePotion(Player target, PotionModel potion)
	{
		if (!Hook.ShouldProcurePotion(target.RunState, target.Creature.CombatState, potion, target))
		{
			return PurchaseStatus.FailureForbidden;
		}
		return target.HasOpenPotionSlots ? PurchaseStatus.Success : PurchaseStatus.FailureSpace;
	}

	internal Player GetGiftTarget(ulong targetId) => GetPlayer(targetId);

	internal Task DoLocalGift(ShopGiftMessage message)
	{
		message.Location = _messageBuffer.CurrentLocation;
		_netService.SendMessage(message);
		return ExecuteGift(LocalPlayer, message);
	}

	private void HandleGift(ShopGiftMessage message, ulong senderId)
	{
		TaskHelper.RunSafely(ExecuteGift(GetPlayer(senderId), message));
	}

	private async Task ExecuteGift(Player sender, ShopGiftMessage message)
	{
		Player target = GetPlayer(message.TargetId);
		MerchantInventory inventory = GameCompat.GetInventoryFor(sender);
		List<MerchantEntry> entries = GetGiftableEntries(inventory);
		if (message.EntryIndex < 0 || message.EntryIndex >= entries.Count)
		{
			throw new InvalidOperationException($"Gift from {sender.NetId} refers to shop entry {message.EntryIndex}, but the shop has {entries.Count}.");
		}
		MerchantEntry entry = entries[message.EntryIndex];

		// Same bookkeeping as a normal purchase, done synchronously before any await.
		bool shouldRestock = sender.RunState.CurrentRoom is MerchantRoom && Hook.ShouldRefillMerchantEntry(sender.RunState, entry, sender);
		if (entry is MerchantRelicEntry relicEntry)
		{
			ShopRelicRefreshState.CommitPurchase(relicEntry);
		}
		if (shouldRestock)
		{
			RestockAfterPurchaseMethod.Invoke(entry, new object[] { inventory });
		}
		else
		{
			ClearAfterPurchaseMethod.Invoke(entry, null);
		}
		entry.OnMerchantInventoryUpdated();

		await PlayerCmd.LoseGold(message.GoldCost, sender, GoldLossType.Spent);
		// v0.113 replays purchase hooks on every client; on v0.111 only the buyer runs them (and syncs their results).
		if (GameCompat.HasSynchronizedMerchant || LocalContext.IsMe(sender))
		{
			await Hook.AfterItemPurchased(sender.RunState, sender, entry, message.GoldCost);
		}
		if (LocalContext.IsMe(sender))
		{
			SfxCmd.Play("event:/sfx/ui/shop_purchase");
		}

		await GiveGift(target, message);
		MainFile.Logger.Info($"Gift {message.ItemType} {message.ItemId} from {sender.NetId} to {target.NetId}");
		if (LocalContext.IsMe(target))
		{
			ShopEnhancementNetwork.ShowGiftNotification(target, sender, "gift.item_received_bubble");
		}
	}

	private static async Task GiveGift(Player target, ShopGiftMessage message)
	{
		ModelId modelId = ModelId.Deserialize(message.ItemId);
		switch (message.ItemType)
		{
		case GiftItemType.Card:
		{
			CardModel template = ModelDb.AllCards.FirstOrDefault(c => c.Id == modelId) ?? throw new InvalidOperationException($"Gifted card {message.ItemId} does not exist.");
			ICardScope cardScope = target.RunState;
			CardModel card = cardScope.CreateCard(template, target);
			for (int i = 0; i < message.UpgradeCount; i++)
			{
				CardCmd.Upgrade(card);
			}
			CardPileAddResult result = await CardPileCmd.Add(card, PileType.Deck);
			if (!result.success)
			{
				throw new InvalidOperationException($"Could not add gifted card {message.ItemId} to the deck of player {target.NetId}.");
			}
			if (LocalContext.IsMe(target))
			{
				CardCmd.PreviewCardPileAdd(result);
			}
			break;
		}
		case GiftItemType.Relic:
		{
			RelicModel template = ModelDb.AllRelics.FirstOrDefault(r => r.Id == modelId) ?? throw new InvalidOperationException($"Gifted relic {message.ItemId} does not exist.");
			RelicModel relic = template.ToMutable();
			MerchantPurchasePriceState.Set(relic, message.GoldCost);
			await RelicCmd.Obtain(relic, target);
			break;
		}
		case GiftItemType.Potion:
		{
			PotionModel template = ModelDb.AllPotions.FirstOrDefault(p => p.Id == modelId) ?? throw new InvalidOperationException($"Gifted potion {message.ItemId} does not exist.");
			PotionModel potion = template.ToMutable();
			MerchantPurchasePriceState.Set(potion, message.GoldCost);
			// The sender checked CheckCanReceivePotion before paying, so this only fails if the target's potion slots
			// changed on this client in the meantime; the clients have diverged then, so fail loudly.
			PotionProcureResult result = await PotionCmd.TryToProcure(potion, target);
			if (!result.success)
			{
				throw new InvalidOperationException($"Gifted potion {message.ItemId} could not be given to player {target.NetId} ({result.failureReason}).");
			}
			break;
		}
		default:
			throw new ArgumentOutOfRangeException(nameof(message), message.ItemType, "Unknown gift item type.");
		}
	}

	// ---- Selling ----

	internal Task DoLocalSell(ShopSellMessage message)
	{
		message.Location = _messageBuffer.CurrentLocation;
		_netService.SendMessage(message);
		return ExecuteSell(LocalPlayer, message);
	}

	private void HandleSell(ShopSellMessage message, ulong senderId)
	{
		TaskHelper.RunSafely(ExecuteSell(GetPlayer(senderId), message));
	}

	private static async Task ExecuteSell(Player seller, ShopSellMessage message)
	{
		if (message.IsRelic)
		{
			if (message.Index < 0 || message.Index >= seller.Relics.Count || seller.Relics[message.Index].Id.ToString() != message.ModelId)
			{
				throw new InvalidOperationException($"Player {seller.NetId} sold relic {message.ModelId} at index {message.Index}, which does not match this client's state.");
			}
			RelicModel relic = seller.Relics[message.Index];
			SoldToyBoxState.PendingContinuation? toyBoxContinuation = SoldToyBoxState.Prepare(relic);
			await RelicCmd.Remove(relic);
			SoldToyBoxState.Activate(toyBoxContinuation);
		}
		else
		{
			PotionModel potion = seller.GetPotionAtSlotIndex(message.Index) ?? throw new InvalidOperationException($"Player {seller.NetId} sold the potion in slot {message.Index}, but that slot is empty on this client.");
			if (potion.Id.ToString() != message.ModelId)
			{
				throw new InvalidOperationException($"Player {seller.NetId} sold potion {message.ModelId} from slot {message.Index}, but this client has {potion.Id} there.");
			}
			await PotionCmd.Discard(potion);
		}
		await PlayerCmd.GainGold(message.Price, seller);
	}
}

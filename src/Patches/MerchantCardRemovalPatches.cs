using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ShopEnhancement.Compatibility;
using ShopEnhancement.Network;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(MerchantCardRemovalEntry))]
public static class MerchantCardRemovalPatches
{
	internal sealed class ShopServiceState
	{
		public bool Rolled;

		public bool IsEnchantShop;

		public bool IsGiftCardShop;

		public float Roll;

		public int ShopVisitIndex;

		public int EnchantmentIndex;

		public int EnchantmentAmount;

		public int EnchantCardCount;

		public int EnchantUsesRemaining;

		public int EnchantCost;

		public ulong GiftTargetId;

		public int GiftCardCount;

		public int GiftCost;
	}

	private static readonly ConditionalWeakTable<MerchantCardRemovalEntry, StrongBox<int>> UsageCounts = new();

	private static readonly ConditionalWeakTable<MerchantInventory, ShopServiceState> ServiceStates = new();

	private static readonly Func<EnchantmentModel>[] EnchantmentFactories = new Func<EnchantmentModel>[]
	{
		() => ModelDb.Enchantment<Sharp>().ToMutable(),
		() => ModelDb.Enchantment<Nimble>().ToMutable(),
		() => ModelDb.Enchantment<Swift>().ToMutable()
	};

	private static int _shopVisitCount;

	[HarmonyPatch("CalcCost")]
	[HarmonyPostfix]
	public static void CalcCost_Postfix(MerchantCardRemovalEntry __instance, Player ____player, ref int ____cost)
	{
		if (TryGetCurrentInventory(out var inventory) && inventory.CardRemovalEntry == __instance)
		{
			var state = EnsureServiceState(inventory);
			if (state.IsEnchantShop)
			{
				____cost = state.EnchantCost;
				return;
			}
			if (state.IsGiftCardShop)
			{
				____cost = state.GiftCost;
				return;
			}
		}
		____cost = ComputeScaledCost(ShopEnhancementConfig.RemoveBaseCost, ShopEnhancementConfig.RemoveStepCost, ____player.ExtraFields.CardShopRemovalsUsed);
	}

	/// <summary>
	/// Replaces the card-removal purchase with the enchant / gift-card service when this shop rolled one.
	/// Called from <see cref="LocalPurchasePatches"/> for purchases the local player starts.
	/// Returns false when the purchase was taken over (Harmony prefix convention).
	/// </summary>
	internal static bool TryHandleServicePurchase(MerchantCardRemovalEntry __instance, MerchantInventory? inventory, bool ignoreCost, bool cancelable, ref Task<bool> __result)
	{
		if (inventory == null)
		{
			return true;
		}
		var state = EnsureServiceState(inventory);
		if ((!state.IsEnchantShop && !state.IsGiftCardShop) || inventory.CardRemovalEntry != __instance)
		{
			return true;
		}
		__result = state.IsGiftCardShop ? HandleGiftCardPurchaseAsync(__instance, inventory, state, ignoreCost, cancelable) : HandleEnchantPurchaseAsync(__instance, inventory, state, ignoreCost, cancelable);
		return false;
	}

	[HarmonyPatch("SetUsed")]
	[HarmonyPrefix]
	public static bool SetUsed_Prefix(MerchantCardRemovalEntry __instance)
	{
		if (IsCurrentEntrySpecialMode(__instance))
		{
			return true;
		}
		StrongBox<int> usageCount = UsageCounts.GetOrCreateValue(__instance);
		usageCount.Value++;
		int removeLimitPerShop = ShopEnhancementConfig.RemoveLimitPerShop;
		if (removeLimitPerShop == -1 || usageCount.Value < removeLimitPerShop)
		{
			// Still under the per-shop limit: keep the entry available and just reprice it.
			__instance.CalcCost();
			return false;
		}
		return true;
	}

	private static async Task<bool> HandleEnchantPurchaseAsync(MerchantCardRemovalEntry entry, MerchantInventory inventory, ShopServiceState state, bool ignoreCost, bool cancelable)
	{
		Player player = inventory.Player;
		EnchantmentModel enchantment = ResolveEnchantment(state.EnchantmentIndex);
		List<CardModel> enchantableCards = PileType.Deck.GetPile(player).Cards.Where(enchantment.CanEnchant).ToList();
		if (state.EnchantUsesRemaining <= 0 || enchantableCards.Count == 0)
		{
			entry.SetUsed();
			entry.OnMerchantInventoryUpdated();
			entry.InvokePurchaseFailed(PurchaseStatus.FailureForbidden);
			return false;
		}
		int goldCost = !ignoreCost ? state.EnchantCost : 0;
		if (!ignoreCost && player.Gold < goldCost)
		{
			entry.InvokePurchaseFailed(PurchaseStatus.FailureGold);
			return false;
		}
		if (!await ShopActionSynchronizer.Current.DoLocalService(new ShopServiceMessage
		{
			Cancelable = cancelable,
			GoldCost = goldCost,
			CardCount = 1,
			EnchantmentIndex = state.EnchantmentIndex,
			EnchantmentAmount = state.EnchantmentAmount
		}))
		{
			return false;
		}
		state.EnchantUsesRemaining = Math.Max(0, state.EnchantUsesRemaining - 1);
		bool hasEnchantableCardsLeft = PileType.Deck.GetPile(player).Cards.Any(enchantment.CanEnchant);
		if (state.EnchantUsesRemaining == 0 || !hasEnchantableCardsLeft)
		{
			entry.SetUsed();
		}
		entry.InvokePurchaseCompleted(entry);
		return true;
	}

	private static async Task<bool> HandleGiftCardPurchaseAsync(MerchantCardRemovalEntry entry, MerchantInventory inventory, ShopServiceState state, bool ignoreCost, bool cancelable)
	{
		Player player = inventory.Player;
		Player? target = player.RunState.Players.FirstOrDefault(p => p.NetId == state.GiftTargetId);
		if (target == null || target.NetId == player.NetId || RunManager.Instance.NetService == null)
		{
			entry.InvokePurchaseFailed(PurchaseStatus.FailureForbidden);
			return false;
		}
		List<CardModel> deckCards = PileType.Deck.GetPile(player).Cards.ToList();
		int cardCount = Math.Min(state.GiftCardCount, deckCards.Count);
		if (cardCount <= 0)
		{
			entry.InvokePurchaseFailed(PurchaseStatus.FailureForbidden);
			return false;
		}
		int goldCost = !ignoreCost ? ComputeGiftServiceCost(cardCount) : 0;
		if (!ignoreCost && player.Gold < goldCost)
		{
			entry.InvokePurchaseFailed(PurchaseStatus.FailureGold);
			return false;
		}
		if (!await ShopActionSynchronizer.Current.DoLocalService(new ShopServiceMessage
		{
			IsGift = true,
			Cancelable = cancelable,
			GoldCost = goldCost,
			CardCount = cardCount,
			TargetId = target.NetId
		}))
		{
			return false;
		}
		entry.SetUsed();
		entry.InvokePurchaseCompleted(entry);
		return true;
	}

	private static bool IsCurrentEntrySpecialMode(MerchantCardRemovalEntry entry)
	{
		if (!TryGetCurrentInventory(out var inventory) || inventory.CardRemovalEntry != entry)
		{
			return false;
		}
		var state = EnsureServiceState(inventory);
		return state.IsEnchantShop || state.IsGiftCardShop;
	}

	private static bool TryGetCurrentInventory([NotNullWhen(true)] out MerchantInventory? inventory)
	{
		return GameCompat.TryGetLocalInventory(out inventory);
	}

	// Rolls (once per inventory) whether this shop's card-removal slot is replaced by an enchant or gift service.
	internal static ShopServiceState EnsureServiceState(MerchantInventory inventory)
	{
		var state = ServiceStates.GetOrCreateValue(inventory);
		if (state.Rolled)
		{
			return state;
		}
		state.Rolled = true;
		state.ShopVisitIndex = Math.Max(1, _shopVisitCount);
		state.IsEnchantShop = false;
		state.IsGiftCardShop = false;
		if (!ShopEnhancementConfig.EnableRemovalEnchantRandom)
		{
			return state;
		}
		if (state.ShopVisitIndex < Math.Max(1, ShopEnhancementConfig.EnchantStartShopVisit))
		{
			return state;
		}
		string stringSeed = inventory.Player.RunState.Rng.StringSeed;
		state.Roll = ComputeRoll(stringSeed, state.ShopVisitIndex, "shop-service-mode");
		float replaceChance = Mathf.Clamp(ShopEnhancementConfig.EnchantReplaceChance, 0f, 1f);
		if (state.Roll >= replaceChance)
		{
			return state;
		}
		bool enableEnchantService = ShopEnhancementConfig.EnableEnchantService;
		bool enableGiftService = ShopEnhancementConfig.EnableGiftMode && ShopEnhancementConfig.EnableRandomTeammateGiftService;
		if (!enableEnchantService && !enableGiftService)
		{
			return state;
		}
		if (enableEnchantService && enableGiftService)
		{
			if (ComputeDerivedRoll(state.Roll, "shop-service-type") < 0.5f)
			{
				if (TryBuildGiftServiceState(inventory, state))
				{
					return state;
				}
				if (TryBuildEnchantServiceState(inventory, state))
				{
					return state;
				}
			}
			else
			{
				if (TryBuildEnchantServiceState(inventory, state))
				{
					return state;
				}
				if (TryBuildGiftServiceState(inventory, state))
				{
					return state;
				}
			}
		}
		else if (enableGiftService)
		{
			if (TryBuildGiftServiceState(inventory, state))
			{
				return state;
			}
		}
		else if (enableEnchantService && TryBuildEnchantServiceState(inventory, state))
		{
			return state;
		}
		state.IsEnchantShop = false;
		state.IsGiftCardShop = false;
		return state;
	}

	private static bool TryBuildEnchantServiceState(MerchantInventory inventory, ShopServiceState state)
	{
		state.IsEnchantShop = true;
		state.IsGiftCardShop = false;
		float typeRoll = ComputeDerivedRoll(state.Roll, "shop-enchant-type");
		state.EnchantmentIndex = Math.Clamp((int)(typeRoll * EnchantmentFactories.Length), 0, EnchantmentFactories.Length - 1);
		state.EnchantmentAmount = RollIntInRange(ShopEnhancementConfig.EnchantAmountRange, ComputeDerivedRoll(state.Roll, "shop-enchant-amount"));
		int rolledCardCount = RollIntInRange(ShopEnhancementConfig.EnchantCardCountRange, ComputeDerivedRoll(state.Roll, "shop-enchant-count"));
		int enchantableCount = CountEnchantableCards(inventory, state.EnchantmentIndex);
		if (enchantableCount <= 0)
		{
			state.IsEnchantShop = false;
			state.EnchantCardCount = 0;
			state.EnchantUsesRemaining = 0;
			state.EnchantCost = 0;
			return false;
		}
		state.EnchantCardCount = Math.Max(1, Math.Min(rolledCardCount, enchantableCount));
		state.EnchantUsesRemaining = state.EnchantCardCount;
		state.EnchantCost = ComputeEnchantUnitCost();
		return true;
	}

	private static bool TryBuildGiftServiceState(MerchantInventory inventory, ShopServiceState state)
	{
		if (!ShopEnhancementConfig.EnableGiftMode || !ShopEnhancementConfig.EnableRandomTeammateGiftService)
		{
			return false;
		}
		List<Player> availableGiftTargets = GetAvailableGiftTargets(inventory.Player);
		if (availableGiftTargets.Count == 0)
		{
			return false;
		}
		int deckCount = PileType.Deck.GetPile(inventory.Player).Cards.Count;
		if (deckCount <= 0)
		{
			return false;
		}
		state.IsEnchantShop = false;
		state.IsGiftCardShop = true;
		int targetIndex = Math.Clamp((int)(ComputeDerivedRoll(state.Roll, "shop-gift-target") * availableGiftTargets.Count), 0, availableGiftTargets.Count - 1);
		state.GiftTargetId = availableGiftTargets[targetIndex].NetId;
		int rolledCardCount = RollIntInRange(ShopEnhancementConfig.GiftServiceCardCountRange, ComputeDerivedRoll(state.Roll, "shop-gift-count"));
		state.GiftCardCount = Math.Max(1, Math.Min(rolledCardCount, deckCount));
		state.GiftCost = ComputeGiftServiceCost(state.GiftCardCount);
		return true;
	}

	internal static string GetCurrentEnchantName(MerchantInventory inventory)
	{
		var state = EnsureServiceState(inventory);
		if (!state.IsEnchantShop)
		{
			return string.Empty;
		}
		return ResolveEnchantment(state.EnchantmentIndex).Title.GetFormattedText();
	}

	internal static string GetCurrentGiftTargetName(MerchantInventory inventory)
	{
		var state = EnsureServiceState(inventory);
		if (!state.IsGiftCardShop || state.GiftTargetId == 0L)
		{
			return string.Empty;
		}
		Player? target = inventory.Player.RunState.Players.FirstOrDefault(p => p.NetId == state.GiftTargetId);
		if (target == null)
		{
			return state.GiftTargetId.ToString();
		}
		return ShopEnhancementNetwork.DescribePlayer(target);
	}

	internal static EnchantmentModel ResolveEnchantment(int index)
	{
		int clampedIndex = Math.Clamp(index, 0, EnchantmentFactories.Length - 1);
		return EnchantmentFactories[clampedIndex]();
	}

	private static int CountEnchantableCards(MerchantInventory inventory, int enchantmentIndex)
	{
		EnchantmentModel enchantment = ResolveEnchantment(enchantmentIndex);
		return PileType.Deck.GetPile(inventory.Player).Cards.Count(enchantment.CanEnchant);
	}

	private static List<Player> GetAvailableGiftTargets(Player sender)
	{
		IReadOnlyCollection<ulong>? connectedIds = GameCompat.GetConnectedPlayerIds(RunManager.Instance.RunLobby);
		return (from p in sender.RunState.Players
			where p.NetId != sender.NetId
			where connectedIds == null || connectedIds.Count == 0 || connectedIds.Contains(p.NetId)
			orderby p.NetId
			select p).ToList();
	}

	private static int RollIntInRange(Vector2I range, float roll)
	{
		int min = Math.Max(1, Math.Min(range.X, range.Y));
		int span = Math.Max(min, Math.Max(range.X, range.Y)) - min + 1;
		int offset = Math.Min(span - 1, (int)(Mathf.Clamp(roll, 0f, 0.999999f) * span));
		return min + offset;
	}

	private static int ComputeEnchantUnitCost()
	{
		return Math.Max(0, ShopEnhancementConfig.EnchantCost);
	}

	private static int ComputeGiftServiceCost(int cardCount)
	{
		int clampedCount = Math.Max(1, cardCount);
		return ComputeScaledCost(ShopEnhancementConfig.GiftServiceBaseCost, ShopEnhancementConfig.GiftServiceStepCost, clampedCount - 1);
	}

	private static int ComputeScaledCost(int baseCost, int stepCost, int stepCount)
	{
		return (int)Math.Min(Math.Max(0L, baseCost) + Math.Max(0L, stepCost) * Math.Max(0L, stepCount), 2147483647L);
	}

	private static float ComputeRoll(string seed, int shopVisitIndex, string salt)
	{
		return (uint)ComputeStableHash($"{seed}|{shopVisitIndex}|{salt}") % 1000000u / 1000000f;
	}

	private static float ComputeDerivedRoll(float sourceRoll, string salt)
	{
		return (uint)ComputeStableHash($"{sourceRoll:F6}|{salt}") % 1000000u / 1000000f;
	}

	// Deterministic across processes (unlike string.GetHashCode), so the shop rolls depend only on the run seed.
	private static int ComputeStableHash(string value)
	{
		int hash1 = 352654597;
		int hash2 = hash1;
		for (int i = 0; i < value.Length; i += 2)
		{
			hash1 = ((hash1 << 5) + hash1) ^ value[i];
			if (i == value.Length - 1)
			{
				break;
			}
			hash2 = ((hash2 << 5) + hash2) ^ value[i + 1];
		}
		return hash1 + hash2 * 1566083941;
	}

	internal static void IncrementVisit()
	{
		_shopVisitCount++;
	}

	internal static void ResetVisit()
	{
		_shopVisitCount = 0;
	}
}

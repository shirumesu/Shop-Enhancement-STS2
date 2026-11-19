using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Compatibility;

/// <summary>
/// Single place for game APIs that differ between the supported game versions.
/// The mod is compiled against whichever game build is installed and runs on both, so every member
/// that only exists in one version is reached through reflection here.
///
/// v0.111 ("legacy merchant"): <c>MerchantRoom.Inventories</c> holds one inventory per player, each client runs its
///   own purchases locally via <c>MerchantEntry.OnTryPurchaseWrapper</c> and syncs the results afterwards.
/// v0.113 ("synchronized merchant"): inventories live in <c>RunManager.MerchantSynchronizer</c>; the UI calls
///   <c>MerchantSynchronizer.PurchaseLocalEntry</c> (local client only), which broadcasts the entry id and then every
///   client replays <c>MerchantEntry.OnTryPurchaseUnsynchronized</c> for that player.
/// </summary>
internal static class GameCompat
{
	internal const string MerchantSynchronizerTypeName = "MegaCrit.Sts2.Core.Multiplayer.Game.MerchantSynchronizer";

	private static readonly PropertyInfo? MerchantSynchronizerProperty = AccessTools.Property(typeof(RunManager), "MerchantSynchronizer");

	/// <summary>True on v0.113+, where merchant purchases are replicated by entry id.</summary>
	internal static bool HasSynchronizedMerchant { get; } = MerchantSynchronizerProperty != null;

	private static readonly PropertyInfo ConnectedPlayerIdsProperty = AccessTools.Property(typeof(RunLobby), "PlayerIds") ?? AccessTools.Property(typeof(RunLobby), "ConnectedPlayerIds") ?? throw new MissingMemberException(typeof(RunLobby).FullName, "PlayerIds/ConnectedPlayerIds");

	private static readonly MethodInfo CreateMerchantCardMethod = ResolveCreateMerchantCard();

	private static readonly bool CreateMerchantCardTakesBlacklist = CreateMerchantCardMethod.GetParameters().Length == 4;

	// v0.111 only
	private static readonly Lazy<PropertyInfo> LegacyRoomProperty = new(() => Require(AccessTools.Property(typeof(NMerchantRoom), "Room"), typeof(NMerchantRoom), "Room"));

	private static readonly Lazy<PropertyInfo> LegacyInventoriesProperty = new(() => Require(AccessTools.Property(typeof(MerchantRoom), "Inventories"), typeof(MerchantRoom), "Inventories"));

	// v0.113 only
	private static readonly Lazy<Type> SynchronizerType = new(() => AccessTools.TypeByName(MerchantSynchronizerTypeName) ?? throw new TypeLoadException(MerchantSynchronizerTypeName));

	private static readonly Lazy<FieldInfo> SynchronizerInventoriesField = new(() => Require(AccessTools.Field(SynchronizerType.Value, "_inventories"), SynchronizerType.Value, "_inventories"));

	private static readonly Lazy<MethodInfo> SynchronizerGetInventoryForMethod = new(() => Require(AccessTools.Method(SynchronizerType.Value, "GetInventoryFor", new[] { typeof(Player) }), SynchronizerType.Value, "GetInventoryFor"));

	private static readonly Lazy<MethodInfo> SynchronizerSetInventoryMethod = new(() => Require(AccessTools.Method(SynchronizerType.Value, "SetMerchantInventory", new[] { typeof(Player), typeof(MerchantInventory) }), SynchronizerType.Value, "SetMerchantInventory"));

	private static readonly Lazy<AccessTools.FieldRef<MerchantCardRemovalEntry, bool>> CardRemovalCancelableField = new(() => AccessTools.FieldRefAccess<MerchantCardRemovalEntry, bool>("_isCancelable"));

	// both versions: MerchantEntry._player (v0.113 also exposes it as MerchantEntry.Player)
	private static readonly AccessTools.FieldRef<MerchantEntry, Player> EntryOwnerField = AccessTools.FieldRefAccess<MerchantEntry, Player>("_player");

	internal static Player GetEntryOwner(MerchantEntry entry) => EntryOwnerField(entry);

	/// <summary>v0.113 only: Lord's Parasol marks the removal uncancelable via <c>SetPurchaseUncancelable</c>.</summary>
	internal static bool IsCardRemovalCancelable(MerchantCardRemovalEntry entry) => CardRemovalCancelableField.Value(entry);

	/// <summary>
	/// All per-player inventories of the merchant the run is currently in (empty when not in a merchant).
	/// The returned list is the game's own list.
	/// </summary>
	internal static List<MerchantInventory> GetMerchantInventories()
	{
		if (HasSynchronizedMerchant)
		{
			return (List<MerchantInventory>)SynchronizerInventoriesField.Value.GetValue(GetSynchronizer())!;
		}
		MerchantRoom? room = GetLegacyRoom();
		return room == null ? new List<MerchantInventory>() : GetLegacyInventories(room);
	}

	/// <summary>All inventories of the given room. On v0.113 they live in the run's merchant synchronizer.</summary>
	internal static List<MerchantInventory> GetMerchantInventories(MerchantRoom room)
	{
		return HasSynchronizedMerchant
			? (List<MerchantInventory>)SynchronizerInventoriesField.Value.GetValue(GetSynchronizer())!
			: GetLegacyInventories(room);
	}

	/// <summary>The local player's inventory while the merchant screen is shown.</summary>
	internal static bool TryGetLocalInventory([NotNullWhen(true)] out MerchantInventory? inventory)
	{
		inventory = null;
		NMerchantRoom? merchantRoom = NRun.Instance?.MerchantRoom;
		if (merchantRoom == null)
		{
			return false;
		}
		List<MerchantInventory> inventories;
		if (HasSynchronizedMerchant)
		{
			inventories = GetMerchantInventories();
		}
		else
		{
			var room = (MerchantRoom?)LegacyRoomProperty.Value.GetValue(merchantRoom);
			if (room == null)
			{
				return false;
			}
			inventories = GetLegacyInventories(room);
		}
		// Look the inventory up by owner, not by player slot (what the game's GetLocalInventory does): while entering a
		// merchant the game creates the inventories one player at a time, and code running during that (e.g. the
		// card-removal price) would otherwise index past the end of the list on any client that is not slot 0.
		inventory = inventories.FirstOrDefault(candidate => LocalContext.IsMe(candidate.Player));
		return inventory != null;
	}

	internal static MerchantInventory GetInventoryFor(Player player)
	{
		if (HasSynchronizedMerchant)
		{
			return (MerchantInventory)SynchronizerGetInventoryForMethod.Value.Invoke(GetSynchronizer(), new object[] { player })!;
		}
		return GetMerchantInventories().Single(inventory => inventory.Player == player);
	}

	/// <summary>Replaces a player's inventory in the current merchant (used by the shop refresh).</summary>
	internal static void ReplaceInventory(Player player, MerchantInventory newInventory)
	{
		if (HasSynchronizedMerchant)
		{
			SynchronizerSetInventoryMethod.Value.Invoke(GetSynchronizer(), new object[] { player, newInventory });
			return;
		}
		List<MerchantInventory> inventories = GetMerchantInventories();
		int index = inventories.FindIndex(inventory => inventory.Player.NetId == player.NetId);
		if (index >= 0)
		{
			inventories[index] = newInventory;
		}
	}

	private static readonly PropertyInfo? IsBlitzShopProperty = AccessTools.Property(typeof(MerchantInventory), "IsBlitzShop");

	/// <summary>v0.113 Blitz mode uses a different merchant (no cards / relics / potions); v0.111 has none.</summary>
	internal static bool IsBlitzShop(MerchantInventory inventory)
	{
		return IsBlitzShopProperty != null && (bool)IsBlitzShopProperty.GetValue(inventory)!;
	}

	/// <summary>
	/// <c>CardFactory.CreateForMerchant</c>: v0.111 takes the already-filtered options,
	/// v0.113 takes the options plus a blacklist.
	/// </summary>
	internal static CardCreationResult CreateMerchantCard(Player player, IEnumerable<CardModel> options, IEnumerable<CardModel> blacklist, CardType type)
	{
		object[] args = CreateMerchantCardTakesBlacklist
			? new object[] { player, options, blacklist, type }
			: new object[] { player, options.Except(blacklist), type };
		try
		{
			return (CardCreationResult)CreateMerchantCardMethod.Invoke(null, args)!;
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			throw;
		}
	}

	internal static MethodBase ResolveCardRewardButtonCreate()
	{
		return AccessTools.DeclaredMethod(typeof(NCardRewardAlternativeButton), "Create", new[]
		{
			typeof(string),
			typeof(string[])
		}) ?? AccessTools.DeclaredMethod(typeof(NCardRewardAlternativeButton), "Create", new[]
		{
			typeof(string),
			typeof(string)
		}) ?? throw new MissingMethodException(typeof(NCardRewardAlternativeButton).FullName, "Create");
	}

	internal static bool IncludesCancelHotkey(object? value)
	{
		string cancelHotkey = MegaInput.cancel;
		if (value is not string singleHotkey)
		{
			if (value is IEnumerable<string> hotkeys)
			{
				return hotkeys.Contains(cancelHotkey);
			}
			return false;
		}
		return singleHotkey == cancelHotkey;
	}

	internal static IReadOnlyCollection<ulong>? GetConnectedPlayerIds(RunLobby? runLobby)
	{
		if (runLobby == null)
		{
			return null;
		}
		object? value = ConnectedPlayerIdsProperty.GetValue(runLobby);
		if (value is not IReadOnlyCollection<ulong> playerIds)
		{
			if (value is IEnumerable<ulong> playerIdSequence)
			{
				return playerIdSequence.ToArray();
			}
			throw new InvalidOperationException($"Unexpected {ConnectedPlayerIdsProperty.Name} value on {typeof(RunLobby).FullName}.");
		}
		return playerIds;
	}

	private static object GetSynchronizer()
	{
		return MerchantSynchronizerProperty!.GetValue(RunManager.Instance) ?? throw new InvalidOperationException("RunManager.MerchantSynchronizer is null (no run in progress).");
	}

	private static MerchantRoom? GetLegacyRoom()
	{
		NMerchantRoom? merchantRoom = NRun.Instance?.MerchantRoom;
		return merchantRoom == null ? null : (MerchantRoom?)LegacyRoomProperty.Value.GetValue(merchantRoom);
	}

	private static List<MerchantInventory> GetLegacyInventories(MerchantRoom room)
	{
		return (List<MerchantInventory>)LegacyInventoriesProperty.Value.GetValue(room)!;
	}

	private static MethodInfo ResolveCreateMerchantCard()
	{
		MethodInfo? method = typeof(CardFactory).GetMethods(BindingFlags.Public | BindingFlags.Static).SingleOrDefault(m =>
		{
			if (m.Name != "CreateForMerchant")
			{
				return false;
			}
			Type[] parameters = m.GetParameters().Select(p => p.ParameterType).ToArray();
			return parameters.SequenceEqual(new[] { typeof(Player), typeof(IEnumerable<CardModel>), typeof(IEnumerable<CardModel>), typeof(CardType) })
				|| parameters.SequenceEqual(new[] { typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType) });
		});
		return method ?? throw new MissingMethodException(typeof(CardFactory).FullName, "CreateForMerchant(Player, IEnumerable<CardModel>, [IEnumerable<CardModel>,] CardType)");
	}

	private static T Require<T>(T? member, Type type, string name) where T : MemberInfo
	{
		return member ?? throw new MissingMemberException(type.FullName, name);
	}
}

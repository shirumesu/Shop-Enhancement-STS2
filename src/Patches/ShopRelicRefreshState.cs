using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using BaseLib.Patches.Saves;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using ShopEnhancement.Config;

namespace ShopEnhancement.Patches;

internal static class ShopRelicRefreshState
{
	internal sealed class PullScope
	{
		public Player Player { get; }

		public MerchantSession Session { get; }

		public PlayerRefreshState PlayerState { get; }

		public BagSnapshot PlayerBagBefore { get; }

		public BagSnapshot SharedBagBefore { get; }

		public HashSet<string> SeenBefore { get; }

		public HashSet<string> ConsumedBefore { get; }

		public Dictionary<string, LedgerEntry> LedgerBefore { get; }

		public HashSet<string> BatchIds { get; } = new HashSet<string>(StringComparer.Ordinal);

		public List<Reservation> Records { get; } = new List<Reservation>();

		public bool Completed { get; set; }

		public PullScope(Player player, MerchantSession session, PlayerRefreshState playerState)
		{
			Player = player;
			Session = session;
			PlayerState = playerState;
			PlayerBagBefore = BagSnapshot.Capture(player.RelicGrabBag);
			SharedBagBefore = BagSnapshot.Capture(player.RunState.SharedRelicGrabBag);
			SeenBefore = new HashSet<string>(playerState.SeenIds, StringComparer.Ordinal);
			ConsumedBefore = new HashSet<string>(playerState.ConsumedIds, StringComparer.Ordinal);
			LedgerBefore = session.Ledger.Snapshot();
		}
	}

	internal sealed class PullState(PullScope scope, BagSnapshot playerSnapshot, BagSnapshot sharedSnapshot)
	{
		public PullScope Scope { get; } = scope;

		public BagSnapshot PlayerSnapshot { get; } = playerSnapshot;

		public BagSnapshot SharedSnapshot { get; } = sharedSnapshot;
	}

	internal sealed class RefreshTransaction
	{
		private readonly InventoryState _inventory;

		private readonly BagSnapshot _playerBag;

		private readonly BagSnapshot _sharedBag;

		private readonly HashSet<string> _seenIds;

		private readonly HashSet<string> _consumedIds;

		private readonly Dictionary<string, LedgerEntry> _ledger;

		private readonly Dictionary<EntryState, (bool Released, bool Committed)> _entryFlags;

		private readonly bool _inventoryReleased;

		public RefreshTransaction(InventoryState inventory)
		{
			_inventory = inventory;
			Player player = inventory.Inventory.Player;
			PlayerRefreshState playerState = PlayerStates.GetOrCreateValue(player);
			_playerBag = BagSnapshot.Capture(player.RelicGrabBag);
			_sharedBag = BagSnapshot.Capture(player.RunState.SharedRelicGrabBag);
			_seenIds = new HashSet<string>(playerState.SeenIds, StringComparer.Ordinal);
			_consumedIds = new HashSet<string>(playerState.ConsumedIds, StringComparer.Ordinal);
			_ledger = inventory.Session.Ledger.Snapshot();
			_entryFlags = inventory.Entries.Values.ToDictionary(state => state, state => (Released: state.Released, Committed: state.Committed));
			_inventoryReleased = inventory.Released;
		}

		public void Rollback()
		{
			Player player = _inventory.Inventory.Player;
			_playerBag.Restore(player.RelicGrabBag);
			_sharedBag.Restore(player.RunState.SharedRelicGrabBag);
			RestorePlayerState(PlayerStates.GetOrCreateValue(player), _seenIds, _consumedIds);
			_inventory.Session.Ledger.Restore(_ledger);
			foreach (KeyValuePair<EntryState, (bool, bool)> pair in _entryFlags)
			{
				pair.Deconstruct(out var key, out var value);
				(bool Released, bool Committed) flags = value;
				EntryState entryState = key;
				bool released = flags.Released;
				bool committed = flags.Committed;
				entryState.Released = released;
				entryState.Committed = committed;
			}
			_inventory.Released = _inventoryReleased;
		}
	}

	internal sealed class PlayerRefreshState
	{
		public HashSet<string> SeenIds { get; } = new HashSet<string>(StringComparer.Ordinal);

		public HashSet<string> ConsumedIds { get; } = new HashSet<string>(StringComparer.Ordinal);
	}

	private sealed class PlayerRefreshSave
	{
		public int Version { get; set; } = 1;

		public List<string> SeenIds { get; set; } = new List<string>();

		public List<string> ConsumedIds { get; set; } = new List<string>();
	}

	internal sealed class MerchantSession(IRunState runState, ShopRelicRefreshMode mode)
	{
		public IRunState RunState { get; } = runState;

		public ShopRelicRefreshMode Mode { get; } = mode;

		public ReservationLedger Ledger { get; } = new ReservationLedger();
	}

	internal sealed class InventoryState(MerchantInventory inventory, MerchantSession session)
	{
		public MerchantInventory Inventory { get; } = inventory;

		public MerchantSession Session { get; } = session;

		public Dictionary<MerchantRelicEntry, EntryState> Entries { get; } = new Dictionary<MerchantRelicEntry, EntryState>();

		public bool Released { get; set; }
	}

	internal sealed class EntryState(InventoryState inventory, Reservation reservation)
	{
		public InventoryState Inventory { get; } = inventory;

		public Reservation Reservation { get; } = reservation;

		public bool Committed { get; set; }

		public bool Released { get; set; }
	}

	internal sealed class Reservation
	{
		public RelicModel Model { get; }

		public string Id { get; }

		public BagLocation? PlayerLocation { get; }

		public BagLocation? SharedLocation { get; }

		public Reservation(RelicModel model, BagLocation? playerLocation, BagLocation? sharedLocation)
		{
			Model = model;
			Id = IdOf(model);
			PlayerLocation = playerLocation;
			SharedLocation = sharedLocation;
		}
	}

	internal sealed class ReservationLedger
	{
		private readonly Dictionary<string, LedgerEntry> _entries = new Dictionary<string, LedgerEntry>(StringComparer.Ordinal);

		public void Reserve(Reservation reservation)
		{
			if (!_entries.TryGetValue(reservation.Id, out var entry))
			{
				entry = new LedgerEntry();
				_entries[reservation.Id] = entry;
			}
			entry.Count++;
			LedgerEntry ledgerEntry = entry;
			if (ledgerEntry.SharedLocation is null)
			{
				BagLocation? sharedLocation = ledgerEntry.SharedLocation = reservation.SharedLocation;
			}
		}

		public void Commit(string id)
		{
			if (_entries.TryGetValue(id, out var entry))
			{
				entry.Count = Math.Max(0, entry.Count - 1);
				if (entry.Count == 0)
				{
					_entries.Remove(id);
				}
			}
		}

		public BagLocation? Release(string id)
		{
			if (!_entries.TryGetValue(id, out var entry))
			{
				return null;
			}
			entry.Count = Math.Max(0, entry.Count - 1);
			if (entry.Count > 0)
			{
				return null;
			}
			_entries.Remove(id);
			return entry.SharedLocation ?? BagLocation.Unknown;
		}

		public Dictionary<string, LedgerEntry> Snapshot()
		{
			return _entries.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.Ordinal);
		}

		public void Restore(Dictionary<string, LedgerEntry> snapshot)
		{
			_entries.Clear();
			foreach (var (id, entry) in snapshot)
			{
				_entries[id] = entry.Clone();
			}
		}
	}

	internal sealed class LedgerEntry
	{
		public int Count { get; set; }

		public BagLocation? SharedLocation { get; set; }

		public LedgerEntry Clone()
		{
			return new LedgerEntry
			{
				Count = Count,
				SharedLocation = SharedLocation
			};
		}
	}

	internal sealed class BagSnapshot
	{
		private readonly Dictionary<RelicRarity, List<RelicModel>> _deques;

		private readonly List<RelicModel> _fallback;

		private BagSnapshot(Dictionary<RelicRarity, List<RelicModel>> deques, List<RelicModel> fallback)
		{
			_deques = deques;
			_fallback = fallback;
		}

		public static BagSnapshot Capture(RelicGrabBag bag)
		{
			return new BagSnapshot(BagAccess.GetDeques(bag).ToDictionary(pair => pair.Key, pair => pair.Value.ToList()), BagAccess.GetFallback(bag).ToList());
		}

		public BagLocation? Find(string id)
		{
			foreach (KeyValuePair<RelicRarity, List<RelicModel>> deque in _deques)
			{
				deque.Deconstruct(out var key, out var value);
				RelicRarity rarity = key;
				List<RelicModel> models = value;
				for (int index = models.Count - 1; index >= 0; index--)
				{
					if (IdOf(models[index]) == id)
					{
						return new BagLocation(rarity, index, IsFallback: false);
					}
				}
			}
			for (int index = _fallback.Count - 1; index >= 0; index--)
			{
				if (IdOf(_fallback[index]) == id)
				{
					return new BagLocation(RelicRarity.None, index, IsFallback: true);
				}
			}
			return null;
		}

		public void Restore(RelicGrabBag bag)
		{
			Dictionary<RelicRarity, List<RelicModel>> deques = BagAccess.GetDeques(bag);
			foreach (List<RelicModel> models in deques.Values)
			{
				models.Clear();
			}
			foreach (var (rarity, savedModels) in _deques)
			{
				if (!deques.TryGetValue(rarity, out var models))
				{
					models = deques[rarity] = new List<RelicModel>();
				}
				models.AddRange(savedModels);
			}
			List<RelicModel> fallback = BagAccess.GetFallback(bag);
			fallback.Clear();
			fallback.AddRange(_fallback);
		}
	}

	internal sealed record BagLocation(RelicRarity Rarity, int Index, bool IsFallback)
	{
		public static BagLocation Unknown { get; } = new BagLocation(RelicRarity.None, -1, IsFallback: false);

		[CompilerGenerated]
		private BagLocation(BagLocation original)
		{
			Rarity = original.Rarity;
			Index = original.Index;
			IsFallback = original.IsFallback;
		}
	}

	private static class BagAccess
	{
		public static Dictionary<RelicRarity, List<RelicModel>> GetDeques(RelicGrabBag bag)
		{
			return (Dictionary<RelicRarity, List<RelicModel>>)DequesField.GetValue(bag)!;
		}

		public static List<RelicModel> GetFallback(RelicGrabBag bag)
		{
			return (List<RelicModel>)FallbackField.GetValue(bag)!;
		}

		public static IEnumerable<RelicModel> GetCandidates(RelicGrabBag bag, RelicRarity rarity)
		{
			Dictionary<RelicRarity, List<RelicModel>> deques = GetDeques(bag);
			foreach (RelicRarity chainRarity in GetFallbackChain(rarity))
			{
				if (!deques.TryGetValue(chainRarity, out var models))
				{
					continue;
				}
				foreach (RelicModel model in models)
				{
					yield return model;
				}
			}
			foreach (RelicModel model in GetFallback(bag))
			{
				yield return model;
			}
		}

		public static void MoveRandomEligibleCandidateToBack(RelicGrabBag bag, RelicRarity rarity, Func<RelicModel, bool> filter, Rng rng)
		{
			Dictionary<RelicRarity, List<RelicModel>> deques = GetDeques(bag);
			foreach (RelicRarity chainRarity in GetFallbackChain(rarity))
			{
				if (deques.TryGetValue(chainRarity, out var models) && TryMoveRandomMatchToBack(models, filter, rng))
				{
					return;
				}
			}
			TryMoveRandomMatchToBack(GetFallback(bag), filter, rng);
		}

		public static void InsertIfMissing(RelicGrabBag bag, RelicModel model, BagLocation? location)
		{
			if (ContainsId(bag, IdOf(model)))
			{
				return;
			}
			if (location is not null && location.IsFallback)
			{
				InsertAt(GetFallback(bag), model, location.Index);
				return;
			}
			RelicRarity rarity = location is not null && location.Rarity != RelicRarity.None ? location.Rarity : model.Rarity;
			Dictionary<RelicRarity, List<RelicModel>> deques = GetDeques(bag);
			if (!deques.TryGetValue(rarity, out var models))
			{
				models = deques[rarity] = new List<RelicModel>();
			}
			InsertAt(models, model, location?.Index ?? -1);
		}

		public static void RemoveId(RelicGrabBag bag, string id)
		{
			foreach (List<RelicModel> models in GetDeques(bag).Values)
			{
				models.RemoveAll(model => IdOf(model) == id);
			}
			GetFallback(bag).RemoveAll(model => IdOf(model) == id);
		}

		private static bool ContainsId(RelicGrabBag bag, string id)
		{
			if (!GetDeques(bag).Values.Any(models => models.Any(model => IdOf(model) == id)))
			{
				return GetFallback(bag).Any(model => IdOf(model) == id);
			}
			return true;
		}

		private static bool TryMoveRandomMatchToBack(List<RelicModel> models, Func<RelicModel, bool> filter, Rng rng)
		{
			List<int> matchIndices = new List<int>();
			for (int i = 0; i < models.Count; i++)
			{
				if (filter(models[i]))
				{
					matchIndices.Add(i);
				}
			}
			if (matchIndices.Count == 0)
			{
				return false;
			}
			int index = matchIndices[rng.NextInt(matchIndices.Count)];
			RelicModel match = models[index];
			models.RemoveAt(index);
			models.Add(match);
			return true;
		}

		private static IEnumerable<RelicRarity> GetFallbackChain(RelicRarity rarity)
		{
			for (RelicRarity current = rarity; current != RelicRarity.None; current = current switch
			{
				RelicRarity.Shop => RelicRarity.Common, 
				RelicRarity.Common => RelicRarity.Uncommon, 
				RelicRarity.Uncommon => RelicRarity.Rare, 
				_ => RelicRarity.None, 
			})
			{
				yield return current;
			}
		}

		private static void InsertAt(List<RelicModel> models, RelicModel model, int index)
		{
			int insertIndex = index < 0 ? models.Count : Math.Min(index, models.Count);
			models.Insert(insertIndex, model);
		}
	}

	private const string PlayerSaveId = "ShopEnhancement.player_shop_relic_refresh_state";

	private const int SaveVersion = 1;

	private static readonly FieldInfo DequesField = AccessTools.Field(typeof(RelicGrabBag), "_deques");

	private static readonly FieldInfo FallbackField = AccessTools.Field(typeof(RelicGrabBag), "_mpFallbackDequeue");

	private static readonly ConditionalWeakTable<Player, PlayerRefreshState> PlayerStates = new ConditionalWeakTable<Player, PlayerRefreshState>();

	private static readonly ConditionalWeakTable<Player, MerchantSession> PlayerSessions = new ConditionalWeakTable<Player, MerchantSession>();

	private static readonly ConditionalWeakTable<MerchantInventory, InventoryState> InventoryStates = new ConditionalWeakTable<MerchantInventory, InventoryState>();

	private static readonly ConditionalWeakTable<MerchantRelicEntry, EntryState> EntryStates = new ConditionalWeakTable<MerchantRelicEntry, EntryState>();

	private static readonly AsyncLocal<PullScope?> ActivePullScope = new AsyncLocal<PullScope?>();

	private static bool _registered;

	public static void RegisterSaves()
	{
		if (!_registered)
		{
			_registered = true;
			ExtendedSaveTypes.RegisterListSaveType<string>();
			ExtendedSaveTypes.RegisterObjectSaveType<PlayerRefreshSave>(new Func<JsonSerializerOptions, JsonPropertyInfo>[3]
			{
				ExtendedSaveTypes.PropertyFunc<PlayerRefreshSave, int>("Version"),
				ExtendedSaveTypes.PropertyFunc<PlayerRefreshSave, List<string>>("SeenIds"),
				ExtendedSaveTypes.PropertyFunc<PlayerRefreshSave, List<string>>("ConsumedIds")
			});
			if (!ExtendedSaveTypes.RegisterSavedValue<Player, PlayerRefreshSave>("ShopEnhancement.player_shop_relic_refresh_state", GetPlayerSave, SetPlayerSave, WritePlayerSave, ReadPlayerSave))
			{
				MainFile.Logger.Error("BaseLib does not support Player extended saves; shop relic queue state will not persist.");
			}
		}
	}

	public static void BeginMerchantSession(IRunState runState)
	{
		var session = new MerchantSession(runState, ShopEnhancementConfig.RelicRefreshMode);
		foreach (Player player in runState.Players)
		{
			PlayerSessions.Remove(player);
			PlayerSessions.Add(player, session);
		}
	}

	public static void EndMerchantSession(IRunState? runState)
	{
		if (runState == null)
		{
			return;
		}
		foreach (Player player in runState.Players)
		{
			PlayerSessions.Remove(player);
		}
	}

	public static void ReleaseInventories(IEnumerable<MerchantInventory> inventories)
	{
		foreach (MerchantInventory inventory in inventories.Reverse())
		{
			ReleaseInventory(inventory);
		}
	}

	public static RefreshTransaction? BeginRefresh(MerchantInventory inventory)
	{
		if (!InventoryStates.TryGetValue(inventory, out var state) || state.Session.Mode == ShopRelicRefreshMode.VanillaConsume)
		{
			return null;
		}
		var transaction = new RefreshTransaction(state);
		ReleaseInventory(inventory);
		return transaction;
	}

	public static void RollbackRefresh(RefreshTransaction? transaction)
	{
		transaction?.Rollback();
	}

	public static PullScope? BeginInventoryBuild(Player player)
	{
		return BeginPullScope(player);
	}

	public static void CompleteInventoryBuild(MerchantInventory inventory, PullScope? scope)
	{
		if (scope != null)
		{
			EndScope(scope);
			var state = new InventoryState(inventory, scope.Session);
			InventoryStates.Remove(inventory);
			InventoryStates.Add(inventory, state);
			int bindCount = Math.Min(inventory.RelicEntries.Count, scope.Records.Count);
			for (int i = 0; i < bindCount; i++)
			{
				BindEntry(state, inventory.RelicEntries[i], scope.Records[i]);
			}
			scope.Completed = true;
		}
	}

	public static void CancelPullScope(PullScope? scope)
	{
		if (scope != null && !scope.Completed)
		{
			EndScope(scope);
			scope.PlayerBagBefore.Restore(scope.Player.RelicGrabBag);
			scope.SharedBagBefore.Restore(scope.Player.RunState.SharedRelicGrabBag);
			RestorePlayerState(scope.PlayerState, scope.SeenBefore, scope.ConsumedBefore);
			scope.Session.Ledger.Restore(scope.LedgerBefore);
			scope.Completed = true;
		}
	}

	public static PullScope? BeginRestock(MerchantRelicEntry entry, MerchantInventory? inventory)
	{
		CommitPurchase(entry);
		if (inventory != null)
		{
			return BeginPullScope(inventory.Player);
		}
		return null;
	}

	public static void CompleteRestock(MerchantRelicEntry entry, MerchantInventory? inventory, PullScope? scope)
	{
		if (scope != null)
		{
			EndScope(scope);
			if (inventory != null && InventoryStates.TryGetValue(inventory, out var state) && scope.Records.Count > 0)
			{
				BindEntry(state, entry, scope.Records[0]);
			}
			scope.Completed = true;
		}
	}

	public static void CommitPurchase(MerchantRelicEntry entry)
	{
		if (!EntryStates.TryGetValue(entry, out var state) || state.Committed || state.Released)
		{
			return;
		}
		PlayerStates.GetOrCreateValue(state.Inventory.Inventory.Player).ConsumedIds.Add(state.Reservation.Id);
		foreach (Player player in state.Inventory.Session.RunState.Players)
		{
			BagAccess.RemoveId(player.RelicGrabBag, state.Reservation.Id);
		}
		BagAccess.RemoveId(state.Inventory.Session.RunState.SharedRelicGrabBag, state.Reservation.Id);
		state.Inventory.Session.Ledger.Commit(state.Reservation.Id);
		state.Committed = true;
		state.Released = true;
	}

	public static void PreparePull(Player player, RelicRarity rarity, ref Func<RelicModel, bool> filter, out PullState? pullState)
	{
		PullScope? scope = ActivePullScope.Value;
		if (scope == null || scope.Player != player || scope.Session.Mode == ShopRelicRefreshMode.VanillaConsume)
		{
			pullState = null;
			return;
		}
		Func<RelicModel, bool> originalFilter = filter;
		BagSnapshot playerSnapshot = BagSnapshot.Capture(player.RelicGrabBag);
		BagSnapshot sharedSnapshot = BagSnapshot.Capture(player.RunState.SharedRelicGrabBag);
		PlayerRefreshState playerState = scope.PlayerState;
		if (scope.Session.Mode == ShopRelicRefreshMode.Queue)
		{
			List<RelicModel> candidates = (from model in BagAccess.GetCandidates(player.RelicGrabBag, rarity)
				where model.IsAllowed(player.RunState) && originalFilter(model) && !scope.BatchIds.Contains(IdOf(model))
				select model).ToList();
			bool hasUnseenCandidate = candidates.Any(model => !playerState.SeenIds.Contains(IdOf(model)));
			// Every eligible candidate has been seen: start a new cycle of the queue.
			if (candidates.Count > 0 && !hasUnseenCandidate)
			{
				foreach (RelicModel candidate in candidates)
				{
					playerState.SeenIds.Remove(IdOf(candidate));
				}
			}
			filter = model => originalFilter(model) && !scope.BatchIds.Contains(IdOf(model)) && !playerState.SeenIds.Contains(IdOf(model));
		}
		else
		{
			filter = model => originalFilter(model) && !scope.BatchIds.Contains(IdOf(model));
			Func<RelicModel, bool> effectiveFilter = filter;
			BagAccess.MoveRandomEligibleCandidateToBack(player.RelicGrabBag, rarity, model => model.IsAllowed(player.RunState) && effectiveFilter(model), player.PlayerRng.Shops);
		}
		pullState = new PullState(scope, playerSnapshot, sharedSnapshot);
	}

	public static void CompletePull(RelicModel result, PullState? pullState)
	{
		if (pullState == null)
		{
			return;
		}
		RelicModel canonical = result.CanonicalInstance;
		string id = IdOf(canonical);
		if (!(canonical.Id == RelicFactory.FallbackRelic.Id))
		{
			PullScope scope = pullState.Scope;
			var reservation = new Reservation(canonical, pullState.PlayerSnapshot.Find(id), pullState.SharedSnapshot.Find(id));
			scope.Records.Add(reservation);
			scope.BatchIds.Add(id);
			scope.Session.Ledger.Reserve(reservation);
			if (scope.Session.Mode == ShopRelicRefreshMode.Queue)
			{
				scope.PlayerState.SeenIds.Add(id);
			}
		}
	}

	private static PullScope? BeginPullScope(Player player)
	{
		if (!PlayerSessions.TryGetValue(player, out var session) || session.Mode == ShopRelicRefreshMode.VanillaConsume)
		{
			return null;
		}
		if (ActivePullScope.Value != null)
		{
			throw new InvalidOperationException("Nested shop relic pull scopes are not supported.");
		}
		var scope = new PullScope(player, session, PlayerStates.GetOrCreateValue(player));
		ActivePullScope.Value = scope;
		return scope;
	}

	private static void EndScope(PullScope scope)
	{
		if (ActivePullScope.Value == scope)
		{
			ActivePullScope.Value = null;
		}
	}

	private static void BindEntry(InventoryState inventory, MerchantRelicEntry entry, Reservation reservation)
	{
		var entryState = new EntryState(inventory, reservation);
		if (inventory.Entries.TryGetValue(entry, out var previousState))
		{
			previousState.Released = true;
		}
		inventory.Entries[entry] = entryState;
		EntryStates.Remove(entry);
		EntryStates.Add(entry, entryState);
	}

	private static void ReleaseInventory(MerchantInventory inventory)
	{
		if (!InventoryStates.TryGetValue(inventory, out var state) || state.Released)
		{
			return;
		}
		foreach (var (entry, entryState) in state.Entries.Reverse())
		{
			if (!entryState.Committed && !entryState.Released)
			{
				bool stillOffersReservedRelic = entry.Model?.Id == entryState.Reservation.Model.Id;
				bool consumed = IsConsumedAnywhere(state.Session.RunState, entryState.Reservation.Id);
				if (stillOffersReservedRelic && !consumed)
				{
					BagAccess.InsertIfMissing(inventory.Player.RelicGrabBag, entryState.Reservation.Model, entryState.Reservation.PlayerLocation);
				}
				BagLocation? sharedLocation = state.Session.Ledger.Release(entryState.Reservation.Id);
				if (stillOffersReservedRelic && sharedLocation != null && !consumed)
				{
					BagAccess.InsertIfMissing(inventory.Player.RunState.SharedRelicGrabBag, entryState.Reservation.Model, sharedLocation);
				}
				entryState.Released = true;
			}
		}
		state.Released = true;
	}

	private static bool IsConsumedAnywhere(IRunState runState, string id)
	{
		foreach (Player player in runState.Players)
		{
			if (player.Relics.Any(relic => IdOf(relic) == id))
			{
				return true;
			}
			if (PlayerStates.TryGetValue(player, out var state) && state.ConsumedIds.Contains(id))
			{
				return true;
			}
		}
		return false;
	}

	private static PlayerRefreshSave? GetPlayerSave(Player player)
	{
		if (!PlayerStates.TryGetValue(player, out var state) || (state.SeenIds.Count == 0 && state.ConsumedIds.Count == 0))
		{
			return null;
		}
		return new PlayerRefreshSave
		{
			Version = 1,
			SeenIds = state.SeenIds.OrderBy(id => id, StringComparer.Ordinal).ToList(),
			ConsumedIds = state.ConsumedIds.OrderBy(id => id, StringComparer.Ordinal).ToList()
		};
	}

	private static void SetPlayerSave(Player player, PlayerRefreshSave? save)
	{
		if (save != null)
		{
			PlayerRefreshState state = PlayerStates.GetOrCreateValue(player);
			state.SeenIds.Clear();
			state.ConsumedIds.Clear();
			state.SeenIds.UnionWith(save.SeenIds ?? new List<string>());
			state.ConsumedIds.UnionWith(save.ConsumedIds ?? new List<string>());
		}
	}

	private static void WritePlayerSave(PlayerRefreshSave save, PacketWriter writer)
	{
		writer.WriteInt(save.Version, 16);
		WriteStrings(writer, save.SeenIds);
		WriteStrings(writer, save.ConsumedIds);
	}

	private static PlayerRefreshSave ReadPlayerSave(PacketReader reader)
	{
		return new PlayerRefreshSave
		{
			Version = reader.ReadInt(16),
			SeenIds = ReadStrings(reader),
			ConsumedIds = ReadStrings(reader)
		};
	}

	private static void WriteStrings(PacketWriter writer, List<string> values)
	{
		writer.WriteInt(values.Count, 16);
		foreach (string value in values)
		{
			writer.WriteString(value);
		}
	}

	private static List<string> ReadStrings(PacketReader reader)
	{
		int count = reader.ReadInt(16);
		List<string> values = new List<string>(count);
		for (int i = 0; i < count; i++)
		{
			values.Add(reader.ReadString());
		}
		return values;
	}

	private static string IdOf(RelicModel relic)
	{
		return relic.Id.ToString();
	}

	private static void RestorePlayerState(PlayerRefreshState state, HashSet<string> seenIds, HashSet<string> consumedIds)
	{
		state.SeenIds.Clear();
		state.SeenIds.UnionWith(seenIds);
		state.ConsumedIds.Clear();
		state.ConsumedIds.UnionWith(consumedIds);
	}
}

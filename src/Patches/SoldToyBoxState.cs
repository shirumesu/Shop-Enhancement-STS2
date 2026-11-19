using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using BaseLib.Patches.Saves;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Patches;

internal static class SoldToyBoxState
{
	internal sealed record PendingContinuation(Player Player, ToyBoxLifecycle Lifecycle);

	internal sealed class ToyBoxLifecycle(int combatsSeen, int combatsPerMelt, int maxCombats)
	{
		public int CombatsSeen { get; set; } = combatsSeen;

		public int CombatsPerMelt { get; } = combatsPerMelt;

		public int MaxCombats { get; } = maxCombats;

		public bool IsComplete => CombatsSeen >= MaxCombats;
	}

	private sealed class PlayerState
	{
		public List<ToyBoxLifecycle> Lifecycles { get; } = new List<ToyBoxLifecycle>();
	}

	private sealed class PlayerSave
	{
		public int Version { get; set; } = 1;

		public List<ToyBoxLifecycleSave> Lifecycles { get; set; } = new List<ToyBoxLifecycleSave>();
	}

	private sealed class ToyBoxLifecycleSave
	{
		public int CombatsSeen { get; set; }

		public int CombatsPerMelt { get; set; }

		public int MaxCombats { get; set; }

		public static ToyBoxLifecycleSave FromState(ToyBoxLifecycle lifecycle)
		{
			return new ToyBoxLifecycleSave
			{
				CombatsSeen = lifecycle.CombatsSeen,
				CombatsPerMelt = lifecycle.CombatsPerMelt,
				MaxCombats = lifecycle.MaxCombats
			};
		}

		public static ToyBoxLifecycle ToState(ToyBoxLifecycleSave save)
		{
			return new ToyBoxLifecycle(Math.Max(0, save.CombatsSeen), Math.Max(1, save.CombatsPerMelt), Math.Max(1, save.MaxCombats));
		}
	}

	private const string PlayerSaveId = "ShopEnhancement.sold_toy_box_state";

	private const int SaveVersion = 1;

	private static readonly ConditionalWeakTable<Player, PlayerState> PlayerStates = new ConditionalWeakTable<Player, PlayerState>();

	private static bool _registered;

	public static void RegisterSaves()
	{
		if (!_registered)
		{
			_registered = true;
			ExtendedSaveTypes.RegisterObjectSaveType<ToyBoxLifecycleSave>(new Func<JsonSerializerOptions, JsonPropertyInfo>[3]
			{
				ExtendedSaveTypes.PropertyFunc<ToyBoxLifecycleSave, int>("CombatsSeen"),
				ExtendedSaveTypes.PropertyFunc<ToyBoxLifecycleSave, int>("CombatsPerMelt"),
				ExtendedSaveTypes.PropertyFunc<ToyBoxLifecycleSave, int>("MaxCombats")
			});
			ExtendedSaveTypes.RegisterListSaveType<ToyBoxLifecycleSave>();
			ExtendedSaveTypes.RegisterObjectSaveType<PlayerSave>(new Func<JsonSerializerOptions, JsonPropertyInfo>[2]
			{
				ExtendedSaveTypes.PropertyFunc<PlayerSave, int>("Version"),
				ExtendedSaveTypes.PropertyFunc<PlayerSave, List<ToyBoxLifecycleSave>>("Lifecycles")
			});
			if (!ExtendedSaveTypes.RegisterSavedValue<Player, PlayerSave>("ShopEnhancement.sold_toy_box_state", GetPlayerSave, SetPlayerSave, WritePlayerSave, ReadPlayerSave))
			{
				MainFile.Logger.Error("BaseLib does not support Player extended saves; sold Toy Box state will not persist.");
			}
		}
	}

	public static PendingContinuation? Prepare(RelicModel relic)
	{
		if (!(relic is ToyBox { IsUsedUp: false } toyBox))
		{
			return null;
		}
		int combatsPerMelt = Math.Max(1, toyBox.DynamicVars["Combats"].IntValue);
		int relicCount = Math.Max(1, toyBox.DynamicVars["Relics"].IntValue);
		int maxCombats = (int)Math.Min(int.MaxValue, (long)combatsPerMelt * relicCount);
		var lifecycle = new ToyBoxLifecycle(Math.Clamp(toyBox.CombatsSeen, 0, maxCombats), combatsPerMelt, maxCombats);
		return new PendingContinuation(toyBox.Owner, lifecycle);
	}

	public static void Activate(PendingContinuation? continuation)
	{
		if (!(continuation == null) && !continuation.Lifecycle.IsComplete)
		{
			PlayerStates.GetOrCreateValue(continuation.Player).Lifecycles.Add(continuation.Lifecycle);
		}
	}

	public static async Task ContinueAfterCombatEnd(Task originalTask, IRunState runState)
	{
		await originalTask;
		foreach (Player player in runState.Players)
		{
			await AdvancePlayer(player);
		}
	}

	private static async Task AdvancePlayer(Player player)
	{
		if (!PlayerStates.TryGetValue(player, out var state))
		{
			return;
		}
		int index = 0;
		while (index < state.Lifecycles.Count)
		{
			ToyBoxLifecycle lifecycle = state.Lifecycles[index];
			lifecycle.CombatsSeen++;
			bool shouldMelt = lifecycle.CombatsSeen % lifecycle.CombatsPerMelt == 0;
			if (lifecycle.IsComplete)
			{
				state.Lifecycles.RemoveAt(index);
			}
			else
			{
				index++;
			}
			if (shouldMelt)
			{
				RelicModel? waxRelic = player.Relics.FirstOrDefault(relic => relic.IsWax && !relic.IsMelted);
				if (waxRelic != null)
				{
					await RelicCmd.Melt(waxRelic);
					await Cmd.CustomScaledWait(0.5f, 0.75f);
				}
			}
		}
		if (state.Lifecycles.Count == 0)
		{
			PlayerStates.Remove(player);
		}
	}

	private static PlayerSave? GetPlayerSave(Player player)
	{
		if (!PlayerStates.TryGetValue(player, out var state) || state.Lifecycles.Count == 0)
		{
			return null;
		}
		return new PlayerSave
		{
			Version = 1,
			Lifecycles = state.Lifecycles.Select(ToyBoxLifecycleSave.FromState).ToList()
		};
	}

	private static void SetPlayerSave(Player player, PlayerSave? save)
	{
		PlayerStates.Remove(player);
		if (save != null && save.Version == 1 && save.Lifecycles.Count != 0)
		{
			var state = new PlayerState();
			state.Lifecycles.AddRange(from lifecycle in save.Lifecycles.Select(ToyBoxLifecycleSave.ToState)
				where !lifecycle.IsComplete
				select lifecycle);
			if (state.Lifecycles.Count > 0)
			{
				PlayerStates.Add(player, state);
			}
		}
	}

	private static void WritePlayerSave(PlayerSave save, PacketWriter writer)
	{
		writer.WriteInt(save.Version);
		writer.WriteInt(save.Lifecycles.Count);
		foreach (ToyBoxLifecycleSave lifecycle in save.Lifecycles)
		{
			writer.WriteInt(lifecycle.CombatsSeen);
			writer.WriteInt(lifecycle.CombatsPerMelt);
			writer.WriteInt(lifecycle.MaxCombats);
		}
	}

	private static PlayerSave ReadPlayerSave(PacketReader reader)
	{
		var save = new PlayerSave
		{
			Version = reader.ReadInt()
		};
		int count = reader.ReadInt();
		for (int i = 0; i < count; i++)
		{
			save.Lifecycles.Add(new ToyBoxLifecycleSave
			{
				CombatsSeen = reader.ReadInt(),
				CombatsPerMelt = reader.ReadInt(),
				MaxCombats = reader.ReadInt()
			});
		}
		return save;
	}
}

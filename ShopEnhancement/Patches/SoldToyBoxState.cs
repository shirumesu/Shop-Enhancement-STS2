using System.Runtime.CompilerServices;
using BaseLib.Patches.Saves;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Patches;

internal static class SoldToyBoxState
{
    private const string PlayerSaveId = "ShopEnhancement.sold_toy_box_state";
    private const int SaveVersion = 1;

    private static readonly ConditionalWeakTable<Player, PlayerState> PlayerStates = new();
    private static bool _registered;

    public static void RegisterSaves()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        ExtendedSaveTypes.RegisterObjectSaveType<ToyBoxLifecycleSave>(
            ExtendedSaveTypes.PropertyFunc<ToyBoxLifecycleSave, int>(nameof(ToyBoxLifecycleSave.CombatsSeen)),
            ExtendedSaveTypes.PropertyFunc<ToyBoxLifecycleSave, int>(nameof(ToyBoxLifecycleSave.CombatsPerMelt)),
            ExtendedSaveTypes.PropertyFunc<ToyBoxLifecycleSave, int>(nameof(ToyBoxLifecycleSave.MaxCombats)));
        ExtendedSaveTypes.RegisterListSaveType<ToyBoxLifecycleSave>();
        ExtendedSaveTypes.RegisterObjectSaveType<PlayerSave>(
            ExtendedSaveTypes.PropertyFunc<PlayerSave, int>(nameof(PlayerSave.Version)),
            ExtendedSaveTypes.PropertyFunc<PlayerSave, List<ToyBoxLifecycleSave>>(nameof(PlayerSave.Lifecycles)));

        bool registered = ExtendedSaveTypes.RegisterSavedValue<Player, PlayerSave>(
            PlayerSaveId,
            GetPlayerSave,
            SetPlayerSave,
            WritePlayerSave,
            ReadPlayerSave);
        if (!registered)
        {
            MainFile.Logger.Error("BaseLib does not support Player extended saves; sold Toy Box state will not persist.");
        }
    }

    public static PendingContinuation? Prepare(RelicModel relic)
    {
        if (relic is not ToyBox toyBox || toyBox.IsUsedUp)
        {
            return null;
        }

        int combatsPerMelt = Math.Max(1, toyBox.DynamicVars["Combats"].IntValue);
        int relicCount = Math.Max(1, toyBox.DynamicVars["Relics"].IntValue);
        int maxCombats = (int)Math.Min(int.MaxValue, (long)combatsPerMelt * relicCount);
        ToyBoxLifecycle lifecycle = new(
            Math.Clamp(toyBox.CombatsSeen, 0, maxCombats),
            combatsPerMelt,
            maxCombats);
        return new PendingContinuation(toyBox.Owner, lifecycle);
    }

    public static void Activate(PendingContinuation? continuation)
    {
        if (continuation == null || continuation.Lifecycle.IsComplete)
        {
            return;
        }

        PlayerStates.GetOrCreateValue(continuation.Player).Lifecycles.Add(continuation.Lifecycle);
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
        if (!PlayerStates.TryGetValue(player, out PlayerState? state))
        {
            return;
        }

        for (int index = 0; index < state.Lifecycles.Count;)
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

            if (!shouldMelt)
            {
                continue;
            }

            RelicModel? waxRelic = player.Relics.FirstOrDefault(relic => relic.IsWax && !relic.IsMelted);
            if (waxRelic != null)
            {
                await RelicCmd.Melt(waxRelic);
                await Cmd.CustomScaledWait(0.5f, 0.75f);
            }
        }

        if (state.Lifecycles.Count == 0)
        {
            PlayerStates.Remove(player);
        }
    }

    private static PlayerSave? GetPlayerSave(Player player)
    {
        if (!PlayerStates.TryGetValue(player, out PlayerState? state) || state.Lifecycles.Count == 0)
        {
            return null;
        }

        return new PlayerSave
        {
            Version = SaveVersion,
            Lifecycles = state.Lifecycles.Select(ToyBoxLifecycleSave.FromState).ToList()
        };
    }

    private static void SetPlayerSave(Player player, PlayerSave? save)
    {
        PlayerStates.Remove(player);
        if (save == null || save.Version != SaveVersion || save.Lifecycles.Count == 0)
        {
            return;
        }

        PlayerState state = new();
        state.Lifecycles.AddRange(save.Lifecycles
            .Select(ToyBoxLifecycleSave.ToState)
            .Where(lifecycle => !lifecycle.IsComplete));
        if (state.Lifecycles.Count > 0)
        {
            PlayerStates.Add(player, state);
        }
    }

    private static void WritePlayerSave(PlayerSave save, PacketWriter writer)
    {
        writer.WriteInt(save.Version, 32);
        writer.WriteInt(save.Lifecycles.Count, 32);
        foreach (ToyBoxLifecycleSave lifecycle in save.Lifecycles)
        {
            writer.WriteInt(lifecycle.CombatsSeen, 32);
            writer.WriteInt(lifecycle.CombatsPerMelt, 32);
            writer.WriteInt(lifecycle.MaxCombats, 32);
        }
    }

    private static PlayerSave ReadPlayerSave(PacketReader reader)
    {
        PlayerSave save = new()
        {
            Version = reader.ReadInt(32)
        };
        int count = reader.ReadInt(32);
        for (int index = 0; index < count; index++)
        {
            save.Lifecycles.Add(new ToyBoxLifecycleSave
            {
                CombatsSeen = reader.ReadInt(32),
                CombatsPerMelt = reader.ReadInt(32),
                MaxCombats = reader.ReadInt(32)
            });
        }

        return save;
    }

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
        public List<ToyBoxLifecycle> Lifecycles { get; } = [];
    }

    private sealed class PlayerSave
    {
        public int Version { get; set; } = SaveVersion;
        public List<ToyBoxLifecycleSave> Lifecycles { get; set; } = [];
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
            return new ToyBoxLifecycle(
                Math.Max(0, save.CombatsSeen),
                Math.Max(1, save.CombatsPerMelt),
                Math.Max(1, save.MaxCombats));
        }
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd),
    typeof(IRunState), typeof(ICombatState), typeof(CombatRoom))]
internal static class SoldToyBoxCombatEndPatch
{
    [HarmonyPostfix]
    private static void Postfix(IRunState runState, ref Task __result)
    {
        __result = SoldToyBoxState.ContinueAfterCombatEnd(__result, runState);
    }
}

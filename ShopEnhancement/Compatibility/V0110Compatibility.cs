using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ShopEnhancement.Compatibility;

// Remove v0.107.1 fallbacks when min_game_version is raised to 0.110.1 or newer.
internal static class V0110Compatibility
{
    private static readonly PropertyInfo ConnectedPlayerIdsProperty =
        AccessTools.Property(typeof(RunLobby), "PlayerIds")
        ?? AccessTools.Property(typeof(RunLobby), "ConnectedPlayerIds")
        ?? throw new MissingMemberException(typeof(RunLobby).FullName, "PlayerIds/ConnectedPlayerIds");

    internal static MethodBase ResolveCardRewardButtonCreate()
    {
        return AccessTools.DeclaredMethod(
                   typeof(NCardRewardAlternativeButton),
                   nameof(NCardRewardAlternativeButton.Create),
                   [typeof(string), typeof(string[])])
               ?? AccessTools.DeclaredMethod(
                   typeof(NCardRewardAlternativeButton),
                   nameof(NCardRewardAlternativeButton.Create),
                   [typeof(string), typeof(string)])
               ?? throw new MissingMethodException(
                   typeof(NCardRewardAlternativeButton).FullName,
                   nameof(NCardRewardAlternativeButton.Create));
    }

    internal static bool IncludesCancelHotkey(object? value)
    {
        string cancelHotkey = MegaInput.cancel;
        return value switch
        {
            string hotkey => hotkey == cancelHotkey,
            IEnumerable<string> hotkeys => hotkeys.Contains(cancelHotkey),
            _ => false
        };
    }

    internal static IReadOnlyCollection<ulong>? GetConnectedPlayerIds(RunLobby? runLobby)
    {
        if (runLobby == null)
        {
            return null;
        }

        object? value = ConnectedPlayerIdsProperty.GetValue(runLobby);
        return value switch
        {
            IReadOnlyCollection<ulong> collection => collection,
            IEnumerable<ulong> playerIds => playerIds.ToArray(),
            _ => throw new InvalidOperationException(
                $"Unexpected {ConnectedPlayerIdsProperty.Name} value on {typeof(RunLobby).FullName}.")
        };
    }
}

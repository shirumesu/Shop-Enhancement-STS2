using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Patches;

[HarmonyPatch]
public static class ShopNoPurchasePatches
{
    private static readonly HashSet<ulong> PlayersPurchasedInCurrentShop = new();

    // Reset flag when entering a merchant room
    [HarmonyPatch(typeof(MerchantRoom), nameof(MerchantRoom.EnterInternal))]
    [HarmonyPostfix]
    public static void EnterInternal_Postfix()
    {
        PlayersPurchasedInCurrentShop.Clear();
    }

    // Set flag when an item is purchased
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterItemPurchased))]
    [HarmonyPostfix]
    public static void AfterItemPurchased_Postfix(Player player)
    {
        PlayersPurchasedInCurrentShop.Add(player.NetId);
    }

    // Check flag and reward when exiting
    [HarmonyPatch(typeof(MerchantRoom), nameof(MerchantRoom.Exit))]
    [HarmonyPrefix]
    public static void Exit_Prefix(IRunState? runState)
    {
        if (!ShopEnhancementConfig.EnableNoPurchaseReward) return;

        // Ensure runState and player are valid
        if (runState == null) return;
        
        // We need to find the local player or the player exiting.
        // runState has Players.
        // Assuming single player logic or applying to the local player context if possible.
        // MerchantRoom.Exit is called on the client.
        // But GainGold is a command.
        
        // Let's iterate players or find "Me".
        // MegaCrit.Sts2.Core.Context.LocalContext.GetMe(runState) is useful.
        
        Player? player = MegaCrit.Sts2.Core.Context.LocalContext.GetMe(runState);
        if (player == null) return;
        if (PlayersPurchasedInCurrentShop.Contains(player.NetId)) return;

        // Give Gold
        // We fire it as a command. It might be processed after the screen hide started, 
        // but the gold change should persist.
        TaskHelper.RunSafely(PlayerCmd.GainGold(ShopEnhancementConfig.NoPurchaseRewardGold, player));
        
        // Optional: Play a sound to indicate reward
        SfxCmd.Play("event:/sfx/ui/rewards/rewards_gold");
    }
}

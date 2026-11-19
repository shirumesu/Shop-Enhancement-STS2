using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using ShopEnhancement.Config;
using ShopEnhancement.Network;

namespace ShopEnhancement;

/// <summary>
/// Run-wide networking: the host's config is pushed to every client so all players use the same shop rules.
/// Shop actions themselves are replicated by <see cref="ShopActionSynchronizer"/>.
/// </summary>
public static class ShopEnhancementNetwork
{
	// Net services we have registered our handlers on (a new one is created per lobby / run).
	private static readonly ConditionalWeakTable<INetGameService, object> RegisteredNetServices = new();

	private static INetGameService? _broadcastedNetService;

	public static void Initialize()
	{
		RunManager.Instance.RunStarted += OnRunStarted;
	}

	public static void EnsureHandlersRegistered()
	{
		INetGameService? netService = RunManager.Instance.NetService;
		if (netService == null || RegisteredNetServices.TryGetValue(netService, out _))
		{
			return;
		}
		netService.RegisterMessageHandler<SyncConfigMessage>(HandleSyncConfigMessage);
		RegisteredNetServices.Add(netService, new object());
		MainFile.Logger.Info("Registered SyncConfigMessage handler.");
	}

	private static void OnRunStarted(RunState state)
	{
		EnsureHandlersRegistered();
		INetGameService? netService = RunManager.Instance.NetService;
		if (netService != null && netService.Type == NetGameType.Host && netService != _broadcastedNetService)
		{
			BroadcastConfig(netService);
			_broadcastedNetService = netService;
		}
	}

	private static void BroadcastConfig(INetGameService netService)
	{
		string configJson = ConfigManager.SerializeConfigData(ConfigManager.GetCurrentConfig());
		netService.SendMessage(new SyncConfigMessage
		{
			ConfigJson = configJson
		});
		MainFile.Logger.Info("Broadcasted ShopEnhancement config to clients.");
	}

	private static void HandleSyncConfigMessage(SyncConfigMessage msg, ulong senderId)
	{
		if (RunManager.Instance.NetService.Type == NetGameType.Host)
		{
			return;
		}
		MainFile.Logger.Info($"Received config sync from {senderId}");
		// Running with a different config than the host would make every shop action diverge, so a config we
		// cannot apply is an error, not something to skip.
		ConfigData configData = ConfigManager.DeserializeConfigData(msg.ConfigJson, out _, rejectInvalidValues: true)
			?? throw new InvalidOperationException($"Host config from {senderId} has values outside ConfigSchema; the two players are probably running different ShopEnhancement versions.");
		ConfigManager.ApplyConfig(configData);
		MainFile.Logger.Info("Applied host config.");
	}

	/// <summary>
	/// The player's platform name (e.g. Steam nickname) as the game's UI shows it, BBCode-escaped for rich text and
	/// LocString variables. Falls back to the character name when the platform has no name for this id.
	/// </summary>
	internal static string DescribePlayer(Player player) => DescribePlayer(player, PlatformUtil.GetPlayerName);

	/// <summary>Like <see cref="DescribePlayer(Player)"/>, unescaped, for plain-text labels such as <c>MegaLabel</c>.</summary>
	internal static string DescribePlayerPlainText(Player player) => DescribePlayer(player, PlatformUtil.GetPlayerNameRaw);

	private static string DescribePlayer(Player player, Func<PlatformType, ulong, string> getName)
	{
		string name = getName(RunManager.Instance.NetService.Platform, player.NetId);
		return string.IsNullOrWhiteSpace(name) ? player.Character.Title.GetFormattedText() : name;
	}

	internal static void ShowGiftNotification(Player player, Player sender, string locKey)
	{
		if (player.Creature != null)
		{
			var bubbleText = new LocString("shop_enhancement", locKey);
			bubbleText.Add("0", DescribePlayer(sender));
			NSpeechBubbleVfx.Create(bubbleText.GetFormattedText(), player.Creature, 3.0);
		}
		SfxCmd.Play("event:/sfx/ui/reward_screen_open");
	}

	private static readonly AccessTools.FieldRef<NMerchantInventory, NMerchantDialogue> MerchantDialogueField =
		AccessTools.FieldRefAccess<NMerchantInventory, NMerchantDialogue>("_merchantDialogue");

	private static readonly MethodInfo ShowDialogueLinesMethod = AccessTools.Method(typeof(NMerchantDialogue), "ShowRandom", new[] { typeof(IEnumerable<LocString>) })
		?? throw new MissingMethodException(nameof(NMerchantDialogue), "ShowRandom");

	/// <summary>
	/// Tells the local player, through the merchant's speech bubble in the open shop screen, that
	/// <paramref name="target"/> cannot receive the gift. Call after <see cref="MerchantEntry.InvokePurchaseFailed"/>
	/// so this line replaces the merchant's generic "purchase failed" line.
	/// </summary>
	internal static void ShowGiftRefused(Player target, PurchaseStatus status)
	{
		string locKey = status switch
		{
			PurchaseStatus.FailureSpace => "gift.target_potion_full",
			PurchaseStatus.FailureForbidden => "gift.target_potion_forbidden",
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, "No gift-refused line for this status.")
		};
		var line = new LocString("shop_enhancement", locKey);
		line.Add("0", DescribePlayer(target));
		NMerchantInventory inventoryScreen = (NMerchantRoom.Instance ?? throw new InvalidOperationException("Gift refused outside of a merchant room.")).Inventory;
		ShowDialogueLinesMethod.Invoke(MerchantDialogueField(inventoryScreen), new object[] { new[] { line } });
	}
}

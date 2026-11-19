using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using ShopEnhancement.Config;
using ShopEnhancement.Patches;

namespace ShopEnhancement;

[ModInitializer("Initialize")]
public partial class MainFile : Node
{
	private const string LocTableName = "shop_enhancement";

	// Languages without their own file (including "eng", whose table is en.json) use English.
	private const string FallbackLocPath = "res://ShopEnhancement/localization/en.json";

	private static readonly FieldInfo LocTablesField = AccessTools.Field(typeof(LocManager), "_tables")
		?? throw new MissingFieldException(nameof(LocManager), "_tables");

	public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new MegaCrit.Sts2.Core.Logging.Logger("ShopEnhancement", LogType.Generic);

	public static void Initialize()
	{
		ConfigManager.Load();
		ShopEnhancementBaseLibConfig.Register("ShopEnhancement");
		SellPriceRandomSeeds.RegisterSaves();
		MerchantPurchasePriceState.RegisterSaves();
		ShopRelicRefreshState.RegisterSaves();
		SoldToyBoxState.RegisterSaves();
		new Harmony("ShopEnhancement").PatchAll();
		ShopEnhancementNetwork.Initialize();
	}

	public static void OnLocaleChanged()
	{
		LoadLocalization();
	}

	private static void LoadLocalization()
	{
		string language = LocManager.Instance.Language;
		string path = "res://ShopEnhancement/localization/" + language + ".json";
		if (!Godot.FileAccess.FileExists(path))
		{
			path = FallbackLocPath;
		}
		using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read)
			?? throw new FileNotFoundException($"ShopEnhancement localization is missing from the .pck ({Godot.FileAccess.GetOpenError()}).", path);
		Dictionary<string, string> entries = JsonSerializer.Deserialize<Dictionary<string, string>>(file.GetAsText())
			?? throw new InvalidDataException($"Localization file {path} is empty.");
		var tables = (Dictionary<string, LocTable>)LocTablesField.GetValue(LocManager.Instance)!;
		tables[LocTableName] = new LocTable(LocTableName, entries);
		Logger.Info($"Loaded localization table '{LocTableName}' for language '{language}' from {path}");
	}
}

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BaseLib.Patches.Saves;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace ShopEnhancement.Patches;

internal static class SellPriceRandomSeeds
{
	private sealed class SeedBox
	{
		public int? Value { get; set; }
	}

	private sealed class SellSeedSave
	{
		public int Seed { get; set; }
	}

	private const string RelicSeedSaveId = "ShopEnhancement.sell_relic_seed";

	private const string PotionSeedSaveId = "ShopEnhancement.sell_potion_seed";

	private static readonly ConditionalWeakTable<RelicModel, SeedBox> RelicSeeds = new ConditionalWeakTable<RelicModel, SeedBox>();

	private static readonly ConditionalWeakTable<PotionModel, SeedBox> PotionSeeds = new ConditionalWeakTable<PotionModel, SeedBox>();

	private static bool _registered;

	public static void RegisterSaves()
	{
		if (!_registered)
		{
			_registered = true;
			ExtendedSaveTypes.RegisterObjectSaveType<SellSeedSave>(new Func<JsonSerializerOptions, JsonPropertyInfo>[1] { ExtendedSaveTypes.PropertyFunc<SellSeedSave, int>("Seed") });
			ExtendedSaveTypes.RegisterSavedValue<RelicModel, SellSeedSave>("ShopEnhancement.sell_relic_seed", GetExistingRelicSeedSave, SetRelicSeedSave, WriteSeedSave, ReadSeedSave);
			ExtendedSaveTypes.RegisterSavedValue<PotionModel, SellSeedSave>("ShopEnhancement.sell_potion_seed", GetExistingPotionSeedSave, SetPotionSeedSave, WriteSeedSave, ReadSeedSave);
		}
	}

	public static int GetOrCreateSeed(RelicModel relic)
	{
		return GetOrCreateSeed(RelicSeeds.GetOrCreateValue(relic));
	}

	public static int GetOrCreateSeed(PotionModel potion)
	{
		return GetOrCreateSeed(PotionSeeds.GetOrCreateValue(potion));
	}

	private static int GetOrCreateSeed(SeedBox box)
	{
		if (!box.Value.HasValue)
		{
			box.Value = CreateSeed();
		}
		return box.Value.Value;
	}

	private static SellSeedSave? GetExistingRelicSeedSave(RelicModel relic)
	{
		if (!RelicSeeds.TryGetValue(relic, out var box) || !box.Value.HasValue)
		{
			return null;
		}
		return new SellSeedSave
		{
			Seed = box.Value.Value
		};
	}

	private static void SetRelicSeedSave(RelicModel relic, SellSeedSave? seed)
	{
		if (seed != null)
		{
			RelicSeeds.GetOrCreateValue(relic).Value = seed.Seed;
		}
	}

	private static SellSeedSave? GetExistingPotionSeedSave(PotionModel potion)
	{
		if (!PotionSeeds.TryGetValue(potion, out var box) || !box.Value.HasValue)
		{
			return null;
		}
		return new SellSeedSave
		{
			Seed = box.Value.Value
		};
	}

	private static void SetPotionSeedSave(PotionModel potion, SellSeedSave? seed)
	{
		if (seed != null)
		{
			PotionSeeds.GetOrCreateValue(potion).Value = seed.Seed;
		}
	}

	private static int CreateSeed()
	{
		Span<byte> bytes = stackalloc byte[4];
		RandomNumberGenerator.Fill(bytes);
		return BitConverter.ToInt32(bytes);
	}

	private static void WriteSeedSave(SellSeedSave seed, PacketWriter writer)
	{
		writer.WriteInt(seed.Seed);
	}

	private static SellSeedSave ReadSeedSave(PacketReader reader)
	{
		return new SellSeedSave
		{
			Seed = reader.ReadInt()
		};
	}
}

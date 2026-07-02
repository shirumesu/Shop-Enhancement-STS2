using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using BaseLib.Patches.Saves;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace ShopEnhancement.Patches;

internal static class SellPriceRandomSeeds
{
    private const string RelicSeedSaveId = "ShopEnhancement.sell_relic_seed";
    private const string PotionSeedSaveId = "ShopEnhancement.sell_potion_seed";

    private static readonly ConditionalWeakTable<RelicModel, SeedBox> RelicSeeds = new();
    private static readonly ConditionalWeakTable<PotionModel, SeedBox> PotionSeeds = new();

    private static bool _registered;

    public static void RegisterSaves()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        ExtendedSaveTypes.RegisterObjectSaveType<SellSeedSave>(
            ExtendedSaveTypes.PropertyFunc<SellSeedSave, int>(nameof(SellSeedSave.Seed)));
        ExtendedSaveTypes.RegisterSavedValue<RelicModel, SellSeedSave>(
            RelicSeedSaveId,
            GetExistingRelicSeedSave,
            SetRelicSeedSave,
            WriteSeedSave,
            ReadSeedSave);
        ExtendedSaveTypes.RegisterSavedValue<PotionModel, SellSeedSave>(
            PotionSeedSaveId,
            GetExistingPotionSeedSave,
            SetPotionSeedSave,
            WriteSeedSave,
            ReadSeedSave);
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
        return RelicSeeds.TryGetValue(relic, out var box) && box.Value.HasValue
            ? new SellSeedSave { Seed = box.Value.Value }
            : null;
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
        return PotionSeeds.TryGetValue(potion, out var box) && box.Value.HasValue
            ? new SellSeedSave { Seed = box.Value.Value }
            : null;
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
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        RandomNumberGenerator.Fill(bytes);
        return BitConverter.ToInt32(bytes);
    }

    private static void WriteSeedSave(SellSeedSave seed, PacketWriter writer)
    {
        writer.WriteInt(seed.Seed, 32);
    }

    private static SellSeedSave ReadSeedSave(PacketReader reader)
    {
        return new SellSeedSave { Seed = reader.ReadInt(32) };
    }

    private sealed class SeedBox
    {
        public int? Value { get; set; }
    }

    private sealed class SellSeedSave
    {
        public int Seed { get; set; }
    }
}

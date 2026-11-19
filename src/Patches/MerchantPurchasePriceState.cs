using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BaseLib.Patches.Saves;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace ShopEnhancement.Patches;

internal static class MerchantPurchasePriceState
{
	private sealed class PriceBox
	{
		public int? Value { get; set; }
	}

	private sealed class PurchasePriceSave
	{
		public int Price { get; set; }
	}

	private const string RelicPurchasePriceSaveId = "ShopEnhancement.relic_merchant_purchase_price";

	private const string PotionPurchasePriceSaveId = "ShopEnhancement.potion_merchant_purchase_price";

	private static readonly ConditionalWeakTable<RelicModel, PriceBox> RelicPrices = new ConditionalWeakTable<RelicModel, PriceBox>();

	private static readonly ConditionalWeakTable<PotionModel, PriceBox> PotionPrices = new ConditionalWeakTable<PotionModel, PriceBox>();

	private static bool _registered;

	public static void RegisterSaves()
	{
		if (!_registered)
		{
			_registered = true;
			ExtendedSaveTypes.RegisterObjectSaveType<PurchasePriceSave>(new Func<JsonSerializerOptions, JsonPropertyInfo>[1] { ExtendedSaveTypes.PropertyFunc<PurchasePriceSave, int>("Price") });
			ExtendedSaveTypes.RegisterSavedValue<RelicModel, PurchasePriceSave>("ShopEnhancement.relic_merchant_purchase_price", GetRelicPurchasePriceSave, SetRelicPurchasePriceSave, WritePurchasePriceSave, ReadPurchasePriceSave);
			ExtendedSaveTypes.RegisterSavedValue<PotionModel, PurchasePriceSave>("ShopEnhancement.potion_merchant_purchase_price", GetPotionPurchasePriceSave, SetPotionPurchasePriceSave, WritePurchasePriceSave, ReadPurchasePriceSave);
		}
	}

	public static void Set(RelicModel relic, int price)
	{
		RelicPrices.GetOrCreateValue(relic).Value = Math.Max(0, price);
	}

	public static void Set(PotionModel potion, int price)
	{
		PotionPrices.GetOrCreateValue(potion).Value = Math.Max(0, price);
	}

	public static bool TryGet(RelicModel relic, out int price)
	{
		return TryGet(RelicPrices, relic, out price);
	}

	public static bool TryGet(PotionModel potion, out int price)
	{
		return TryGet(PotionPrices, potion, out price);
	}

	private static bool TryGet<T>(ConditionalWeakTable<T, PriceBox> prices, T item, out int price) where T : class
	{
		if (prices.TryGetValue(item, out var box) && box.Value.HasValue)
		{
			price = box.Value.Value;
			return true;
		}
		price = 0;
		return false;
	}

	private static PurchasePriceSave? GetRelicPurchasePriceSave(RelicModel relic)
	{
		if (!TryGet(relic, out var price))
		{
			return null;
		}
		return new PurchasePriceSave
		{
			Price = price
		};
	}

	private static void SetRelicPurchasePriceSave(RelicModel relic, PurchasePriceSave? save)
	{
		if (save != null)
		{
			Set(relic, save.Price);
		}
	}

	private static PurchasePriceSave? GetPotionPurchasePriceSave(PotionModel potion)
	{
		if (!TryGet(potion, out var price))
		{
			return null;
		}
		return new PurchasePriceSave
		{
			Price = price
		};
	}

	private static void SetPotionPurchasePriceSave(PotionModel potion, PurchasePriceSave? save)
	{
		if (save != null)
		{
			Set(potion, save.Price);
		}
	}

	private static void WritePurchasePriceSave(PurchasePriceSave save, PacketWriter writer)
	{
		writer.WriteInt(save.Price);
	}

	private static PurchasePriceSave ReadPurchasePriceSave(PacketReader reader)
	{
		return new PurchasePriceSave
		{
			Price = reader.ReadInt()
		};
	}
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ShopEnhancement.Patches;

internal sealed record RelicSellEvaluation(bool CanSell, IReadOnlyList<RelicSellBlockReason> Reasons)
{
	public static RelicSellEvaluation Allowed { get; } = new RelicSellEvaluation(CanSell: true, Array.Empty<RelicSellBlockReason>());

	[CompilerGenerated]
	private RelicSellEvaluation(RelicSellEvaluation original)
	{
		CanSell = original.CanSell;
		Reasons = original.Reasons;
	}
}

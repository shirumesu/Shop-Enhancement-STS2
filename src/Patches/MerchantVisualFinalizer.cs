using System;

namespace ShopEnhancement.Patches;

public static class MerchantVisualFinalizer
{
	public static Exception? Handle(Exception? exception)
	{
		if (exception is ObjectDisposedException)
		{
			return null;
		}
		return exception;
	}
}

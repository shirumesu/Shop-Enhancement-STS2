using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace ShopEnhancement.Config;

public static class ConfigShareCode
{
	private const string Prefix = "SE1-";

	private const int MaxCodeLength = 8192;

	private const int MaxDecodedBytes = 65536;

	public static ShareCodeOperationResult Export(ConfigData data, out string code)
	{
		code = "";
		if (!ConfigManager.IsConfigDataValid(data))
		{
			return ShareCodeOperationResult.InvalidData;
		}
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(ConfigManager.SerializeConfigData(data));
			using var compressed = new MemoryStream();
			using (var brotli = new BrotliStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
			{
				brotli.Write(bytes);
			}
			code = Prefix + ToBase64Url(compressed.ToArray());
			return ShareCodeOperationResult.Success;
		}
		catch (Exception ex)
		{
			MainFile.Logger.Error($"Failed to export configuration share code: {ex}");
			return ShareCodeOperationResult.Failed;
		}
	}

	public static ShareCodeOperationResult Import(string? code, out ConfigData? data)
	{
		data = null;
		string trimmedCode = code?.Trim() ?? "";
		if (trimmedCode.Length == 0)
		{
			return ShareCodeOperationResult.EmptyCode;
		}
		if (trimmedCode.Length > MaxCodeLength)
		{
			return ShareCodeOperationResult.InvalidFormat;
		}
		if (!trimmedCode.StartsWith(Prefix, StringComparison.Ordinal))
		{
			// Codes still shaped like "SE<version>-..." are reported as an unsupported version rather than garbage.
			if (!trimmedCode.StartsWith("SE", StringComparison.OrdinalIgnoreCase) || !trimmedCode.Contains('-'))
			{
				return ShareCodeOperationResult.InvalidFormat;
			}
			return ShareCodeOperationResult.UnsupportedVersion;
		}
		try
		{
			string prefixedPayload = trimmedCode;
			int prefixLength = Prefix.Length;
			using var compressed = new MemoryStream(FromBase64Url(prefixedPayload.Substring(prefixLength, prefixedPayload.Length - prefixLength)), writable: false);
			using var brotli = new BrotliStream(compressed, CompressionMode.Decompress);
			using var decompressed = new MemoryStream();
			byte[] buffer = new byte[4096];
			while (true)
			{
				int bytesRead = brotli.Read(buffer);
				if (bytesRead == 0)
				{
					break;
				}
				// Guard against decompression bombs.
				if (decompressed.Length + bytesRead > MaxDecodedBytes)
				{
					return ShareCodeOperationResult.InvalidData;
				}
				decompressed.Write(buffer, 0, bytesRead);
			}
			string json = Encoding.UTF8.GetString(decompressed.ToArray());
			data = ConfigManager.DeserializeConfigData(json, out _, rejectInvalidValues: true);
			return (data == null) ? ShareCodeOperationResult.InvalidData : ShareCodeOperationResult.Success;
		}
		catch (FormatException)
		{
			return ShareCodeOperationResult.InvalidFormat;
		}
		catch (InvalidDataException)
		{
			return ShareCodeOperationResult.InvalidData;
		}
		catch (JsonException)
		{
			return ShareCodeOperationResult.InvalidData;
		}
		catch (Exception ex)
		{
			MainFile.Logger.Error($"Failed to import configuration share code: {ex}");
			return ShareCodeOperationResult.Failed;
		}
	}

	private static string ToBase64Url(byte[] bytes)
	{
		return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}

	private static byte[] FromBase64Url(string value)
	{
		string base64 = value.Replace('-', '+').Replace('_', '/');
		int paddingLength = (4 - base64.Length % 4) % 4;
		return Convert.FromBase64String(base64.PadRight(base64.Length + paddingLength, '='));
	}
}

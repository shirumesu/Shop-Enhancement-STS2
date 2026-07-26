using System.IO.Compression;
using System.Text;

namespace ShopEnhancement.Config;

public enum ShareCodeOperationResult
{
    Success,
    EmptyCode,
    InvalidFormat,
    UnsupportedVersion,
    InvalidData,
    Failed
}

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
            byte[] json = Encoding.UTF8.GetBytes(ConfigManager.SerializeConfigData(data));
            using MemoryStream compressed = new();
            using (BrotliStream compressor = new(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                compressor.Write(json);
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
        string normalizedCode = code?.Trim() ?? "";
        if (normalizedCode.Length == 0)
        {
            return ShareCodeOperationResult.EmptyCode;
        }

        if (normalizedCode.Length > MaxCodeLength)
        {
            return ShareCodeOperationResult.InvalidFormat;
        }

        if (!normalizedCode.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return normalizedCode.StartsWith("SE", StringComparison.OrdinalIgnoreCase)
                && normalizedCode.Contains('-')
                ? ShareCodeOperationResult.UnsupportedVersion
                : ShareCodeOperationResult.InvalidFormat;
        }

        try
        {
            byte[] compressed = FromBase64Url(normalizedCode[Prefix.Length..]);
            using MemoryStream source = new(compressed, writable: false);
            using BrotliStream decompressor = new(source, CompressionMode.Decompress);
            using MemoryStream decoded = new();
            byte[] buffer = new byte[4096];

            while (true)
            {
                int read = decompressor.Read(buffer);
                if (read == 0)
                {
                    break;
                }

                if (decoded.Length + read > MaxDecodedBytes)
                {
                    return ShareCodeOperationResult.InvalidData;
                }

                decoded.Write(buffer, 0, read);
            }

            string json = Encoding.UTF8.GetString(decoded.ToArray());
            data = ConfigManager.DeserializeConfigData(json, out _, rejectInvalidValues: true);
            return data == null
                ? ShareCodeOperationResult.InvalidData
                : ShareCodeOperationResult.Success;
        }
        catch (FormatException)
        {
            return ShareCodeOperationResult.InvalidFormat;
        }
        catch (InvalidDataException)
        {
            return ShareCodeOperationResult.InvalidData;
        }
        catch (System.Text.Json.JsonException)
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
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] FromBase64Url(string value)
    {
        string base64 = value
            .Replace('-', '+')
            .Replace('_', '/');
        int padding = (4 - base64.Length % 4) % 4;
        return Convert.FromBase64String(base64.PadRight(base64.Length + padding, '='));
    }
}

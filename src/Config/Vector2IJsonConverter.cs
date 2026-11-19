using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace ShopEnhancement.Config;

public sealed class Vector2IJsonConverter : JsonConverter<Vector2I>
{
	public override Vector2I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected a range object.");
		}
		int? min = null;
		int? max = null;
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				// A missing Min / Max reads as 0 on purpose: ConfigManager.TryMigrateBrokenRanges detects such ranges
				// in the raw JSON afterwards and replaces them with the schema defaults (old config files lack them).
				return new Vector2I(min.GetValueOrDefault(), max.GetValueOrDefault());
			}
			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				throw new JsonException("Expected a range property.");
			}
			string? propertyName = reader.GetString();
			if (!reader.Read())
			{
				throw new JsonException("Unexpected end of range data.");
			}
			if (propertyName != null && (propertyName.Equals("Min", StringComparison.OrdinalIgnoreCase) || propertyName.Equals("X", StringComparison.OrdinalIgnoreCase)))
			{
				min = reader.GetInt32();
			}
			else if (propertyName != null && (propertyName.Equals("Max", StringComparison.OrdinalIgnoreCase) || propertyName.Equals("Y", StringComparison.OrdinalIgnoreCase)))
			{
				max = reader.GetInt32();
			}
			else
			{
				reader.Skip();
			}
		}
		throw new JsonException("Unexpected end of range data.");
	}

	public override void Write(Utf8JsonWriter writer, Vector2I value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber("Min", value.X);
		writer.WriteNumber("Max", value.Y);
		writer.WriteEndObject();
	}
}

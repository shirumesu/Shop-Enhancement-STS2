using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Network;

/// <summary>
/// "The sender sold the relic at <see cref="Index"/> (or the potion in slot <see cref="Index"/>) for <see cref="Price"/>."
/// Every client replays it. <see cref="ModelId"/> guards against the index pointing at a different item.
/// </summary>
public sealed class ShopSellMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
	public bool ShouldBroadcast => true;

	public NetTransferMode Mode => NetTransferMode.Reliable;

	public LogLevel LogLevel => LogLevel.Info;

	public bool ShouldBuffer => true;

	public RunLocation Location { get; set; }

	public bool IsRelic { get; set; }

	public int Index { get; set; }

	public string ModelId { get; set; } = "";

	public int Price { get; set; }

	public void Serialize(PacketWriter writer)
	{
		writer.Write(Location);
		writer.WriteBool(IsRelic);
		writer.WriteInt(Index);
		writer.WriteString(ModelId);
		writer.WriteInt(Price);
	}

	public void Deserialize(PacketReader reader)
	{
		Location = reader.Read<RunLocation>();
		IsRelic = reader.ReadBool();
		Index = reader.ReadInt();
		ModelId = reader.ReadString();
		Price = reader.ReadInt();
	}
}

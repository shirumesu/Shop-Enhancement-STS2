using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Network;

/// <summary>"The sender refreshed their shop for <see cref="GoldCost"/> gold." Every client replays the refresh.</summary>
public sealed class ShopRefreshMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
	public bool ShouldBroadcast => true;

	public NetTransferMode Mode => NetTransferMode.Reliable;

	public LogLevel LogLevel => LogLevel.Info;

	public bool ShouldBuffer => true;

	public RunLocation Location { get; set; }

	public int GoldCost { get; set; }

	public void Serialize(PacketWriter writer)
	{
		writer.Write(Location);
		writer.WriteInt(GoldCost);
	}

	public void Deserialize(PacketReader reader)
	{
		Location = reader.Read<RunLocation>();
		GoldCost = reader.ReadInt();
	}
}

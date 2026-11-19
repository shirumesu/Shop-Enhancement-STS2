using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Network;

public sealed class ShopServiceMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
	public bool ShouldBroadcast => true;

	public NetTransferMode Mode => NetTransferMode.Reliable;

	public LogLevel LogLevel => LogLevel.Info;

	public bool ShouldBuffer => true;

	public RunLocation Location { get; set; }

	public bool IsGift { get; set; }

	public bool Cancelable { get; set; }

	public int GoldCost { get; set; }

	public int CardCount { get; set; }

	public int EnchantmentIndex { get; set; }

	public int EnchantmentAmount { get; set; }

	public ulong TargetId { get; set; }

	public void Serialize(PacketWriter writer)
	{
		writer.Write(Location);
		writer.WriteBool(IsGift);
		writer.WriteBool(Cancelable);
		writer.WriteInt(GoldCost);
		writer.WriteInt(CardCount);
		writer.WriteInt(EnchantmentIndex);
		writer.WriteInt(EnchantmentAmount);
		writer.WriteULong(TargetId);
	}

	public void Deserialize(PacketReader reader)
	{
		Location = reader.Read<RunLocation>();
		IsGift = reader.ReadBool();
		Cancelable = reader.ReadBool();
		GoldCost = reader.ReadInt();
		CardCount = reader.ReadInt();
		EnchantmentIndex = reader.ReadInt();
		EnchantmentAmount = reader.ReadInt();
		TargetId = reader.ReadULong();
	}
}

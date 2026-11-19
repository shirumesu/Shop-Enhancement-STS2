using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Network;

public enum GiftItemType
{
	Card,
	Relic,
	Potion
}

/// <summary>
/// "The sender bought shop entry <see cref="EntryIndex"/> for <see cref="GoldCost"/> gold and gave the item to
/// <see cref="TargetId"/>." Every client replays it: the sender pays and the entry is cleared / restocked, the target
/// receives the item. The item itself is carried by id so receivers do not depend on their copy of the sender's shop.
/// </summary>
public sealed class ShopGiftMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
	public bool ShouldBroadcast => true;

	public NetTransferMode Mode => NetTransferMode.Reliable;

	public LogLevel LogLevel => LogLevel.Info;

	public bool ShouldBuffer => true;

	public RunLocation Location { get; set; }

	public ulong TargetId { get; set; }

	/// <summary>Index into the sender's <see cref="ShopActionSynchronizer.GetGiftableEntries"/>.</summary>
	public int EntryIndex { get; set; }

	public int GoldCost { get; set; }

	public GiftItemType ItemType { get; set; }

	public string ItemId { get; set; } = "";

	public int UpgradeCount { get; set; }

	public void Serialize(PacketWriter writer)
	{
		writer.Write(Location);
		writer.WriteULong(TargetId);
		writer.WriteInt(EntryIndex);
		writer.WriteInt(GoldCost);
		writer.WriteInt((int)ItemType);
		writer.WriteString(ItemId);
		writer.WriteInt(UpgradeCount);
	}

	public void Deserialize(PacketReader reader)
	{
		Location = reader.Read<RunLocation>();
		TargetId = reader.ReadULong();
		EntryIndex = reader.ReadInt();
		GoldCost = reader.ReadInt();
		ItemType = (GiftItemType)reader.ReadInt();
		ItemId = reader.ReadString();
		UpgradeCount = reader.ReadInt();
	}
}

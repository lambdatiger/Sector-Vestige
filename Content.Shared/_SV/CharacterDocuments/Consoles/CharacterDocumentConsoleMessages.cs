using System.Reflection.Metadata;
using Robust.Shared.Serialization;

namespace Content.Shared._SV.CharacterDocuments.Consoles;

[Serializable, NetSerializable]
public sealed class SelectCharacterDocumentPlayer : BoundUserInterfaceMessage
{
    public NetEntity Player;
}

[Serializable, NetSerializable]
public sealed class SelectCharacterDocument : BoundUserInterfaceMessage
{
    public NetEntity Player;
    public int DocID;
}

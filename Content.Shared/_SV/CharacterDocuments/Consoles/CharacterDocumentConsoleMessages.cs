using System.Reflection.Metadata;
using Robust.Shared.Serialization;

namespace Content.Shared._SV.CharacterDocuments.Consoles;

[Serializable, NetSerializable]
public sealed class SelectCharacterDocumentPlayer : BoundUserInterfaceMessage
{
    public EntityUid Player;
}

[Serializable, NetSerializable]
public sealed class SelectCharacterDocument : BoundUserInterfaceMessage
{
    public EntityUid Player;
    public int DocID;
}

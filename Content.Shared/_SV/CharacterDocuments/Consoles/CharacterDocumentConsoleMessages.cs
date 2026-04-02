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

[Serializable, NetSerializable]
public sealed class CharacterDocumentScan : BoundUserInterfaceMessage
{
    public NetEntity Player;
    public string DocTitle = string.Empty;
}

[Serializable, NetSerializable]
public sealed class CharacterDocumentDelete : BoundUserInterfaceMessage
{
    public NetEntity Player;
    public CharacterDocument? CharacterDocument;
}

[Serializable, NetSerializable]
public sealed class CharacterDocumentDeselect : BoundUserInterfaceMessage
{
}

using Content.Shared.Security;
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
    /// <summary>
    /// Doc type to tag the scanned document as. Null = use the console's primary type.
    /// Multi-type consoles (Central Command) set this to the active tab so scans land
    /// in the tab the user is viewing rather than always the primary type.
    /// </summary>
    public int? DocType;
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

/// <summary>
/// Restores a binned (soft-deleted) document back to normal view. Only honoured on
/// Central Command (bin-access) consoles; the server re-checks access on receipt.
/// </summary>
[Serializable, NetSerializable]
public sealed class CharacterDocumentRestore : BoundUserInterfaceMessage
{
    public NetEntity Player;
    public int DocID;
}

/// <summary>
/// Permanently deletes a single binned (soft-deleted) document — bypassing the retention
/// window. Irreversible. Only honoured on Central Command (bin-access) consoles; the server
/// re-checks access and that the target is actually binned on receipt.
/// </summary>
[Serializable, NetSerializable]
public sealed class CharacterDocumentPurge : BoundUserInterfaceMessage
{
    public NetEntity Player;
    public int DocID;
}

/// <summary>
/// Permanently deletes every binned (soft-deleted) document for the target player — the
/// "empty the recycling bin" action. Irreversible. Only honoured on Central Command
/// (bin-access) consoles; the server re-checks access on receipt.
/// </summary>
[Serializable, NetSerializable]
public sealed class CharacterDocumentEmptyBin : BoundUserInterfaceMessage
{
    public NetEntity Player;
}

[Serializable, NetSerializable]
public sealed class CharacterDocumentPrint : BoundUserInterfaceMessage
{
    public NetEntity Player;
    public CharacterDocument? CharacterDocument;
}

[Serializable, NetSerializable]
public sealed class CharacterDocumentEdit : BoundUserInterfaceMessage
{
    public NetEntity Player;
    public CharacterDocument? CharacterDocument;
}

[Serializable, NetSerializable]
public sealed class CharacterDocumentSecurityStatus : BoundUserInterfaceMessage
{
    public NetEntity Player;
    public SecurityStatus Status;
    public string? Reason;
}

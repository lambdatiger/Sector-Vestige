using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.Security;
using Robust.Shared.Serialization;

namespace Content.Shared._SV.CharacterDocuments.Consoles;

[Serializable, NetSerializable]
public sealed class CharacterDocumentConsoleState : BoundUserInterfaceState
{
    public Dictionary<NetEntity, string> PlayerList = new();
    public NetEntity? SelectedPlayer;
    public Dictionary<int, CharacterDocument>? SelectedPlayerDocuments;
    public CharacterDocument? SelectedDocument;
    public bool PaperInserted;
    public DocumentType DocumentType;
    public SecurityStatus SecurityStatus;
    public string? SecurityReason;

    public CharacterDocumentConsoleState(Dictionary<NetEntity, string> playerlist, NetEntity? selectedplayer,
        Dictionary<int, CharacterDocument>? selectedplayerdocuments, CharacterDocument? selecteddocument, bool paperinserted,
        DocumentType documentType = DocumentType.Employment, SecurityStatus securityStatus = SecurityStatus.None, string? securityReason = null)
    {
        PlayerList = playerlist;
        SelectedPlayer = selectedplayer;
        SelectedPlayerDocuments = selectedplayerdocuments;
        SelectedDocument = selecteddocument;
        PaperInserted = paperinserted;
        DocumentType = documentType;
        SecurityStatus = securityStatus;
        SecurityReason = securityReason;
    }
}

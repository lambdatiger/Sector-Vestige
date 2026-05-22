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
    /// <summary>
    /// Extra types this console covers in addition to <see cref="DocumentType"/>.
    /// Empty for single-type consoles; non-empty for the Central Command terminal.
    /// </summary>
    public List<DocumentType> AdditionalDocumentTypes = new();
    public SecurityStatus SecurityStatus;
    public string? SecurityReason;
    /// <summary>
    /// Selected player's General flavour block (allergies, height, etc). Null if no player selected.
    /// Console UIs render relevant fields based on the active tab (or primary type for single-type consoles).
    /// </summary>
    public CharacterDocumentGeneral? SelectedPlayerGeneral;

    public CharacterDocumentConsoleState(Dictionary<NetEntity, string> playerlist, NetEntity? selectedplayer,
        Dictionary<int, CharacterDocument>? selectedplayerdocuments, CharacterDocument? selecteddocument, bool paperinserted,
        DocumentType documentType = DocumentType.Employment,
        SecurityStatus securityStatus = SecurityStatus.None, string? securityReason = null,
        List<DocumentType>? additionalDocumentTypes = null,
        CharacterDocumentGeneral? selectedPlayerGeneral = null)
    {
        PlayerList = playerlist;
        SelectedPlayer = selectedplayer;
        SelectedPlayerDocuments = selectedplayerdocuments;
        SelectedDocument = selecteddocument;
        PaperInserted = paperinserted;
        DocumentType = documentType;
        SecurityStatus = securityStatus;
        SecurityReason = securityReason;
        AdditionalDocumentTypes = additionalDocumentTypes ?? new List<DocumentType>();
        SelectedPlayerGeneral = selectedPlayerGeneral;
    }
}

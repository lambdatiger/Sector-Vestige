using Content.Shared.Paper;
using Robust.Shared.Serialization;

namespace Content.Shared._SV.CharacterDocuments;

public enum DocumentType
{
    Employment,
    Medical,
    Security,
    CentralCommand,
    Syndicate,
    Admin,
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class CharacterDocument
{
    [DataField]
    public int DocID;

    [DataField]
    public int DocType = (int)DocumentType.Employment;

    [DataField]
    public string DocTitle = string.Empty;

    [DataField]
    public string DocAuthor = string.Empty;

    [DataField]
    public string DocLastEditedBy = string.Empty;

    [DataField]
    public DateTime DocDateLastEdited = DateTime.Today.AddYears(200); //Today plus 200 years

    [DataField]
    public string DocContent = string.Empty;

    [DataField]
    public List<CharacterDocumentStamp> DocStamps = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct CharacterDocumentStamp()
{
    [DataField]
    public StampDisplayInfo DocStamp = default;

    [DataField]
    public string DocStampState = string.Empty;
}

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

[Serializable, NetSerializable]
public sealed partial class CharacterDocument
{
    [DataField("readOnly")]
    public int DocID;
    [DataField("readOnly")]
    public int DocType = (int)DocumentType.Employment;
    [DataField("readOnly")]
    public string DocTitle = null!;
    [DataField("readOnly")]
    public string DocAuthor = null!;
    [DataField("readOnly")]
    public string DocLastEditedBy = "";
    [DataField("readOnly")]
    public DateTime DocDateLastEdited = DateTime.Today.AddYears(200); //Today plus 200 years
    [DataField("readOnly")]
    public string DocContent = "";
    [DataField("readOnly")]
    public List<CharacterDocumentStamp> DocStamps = new();
}

[Serializable, NetSerializable]
public struct CharacterDocumentStamp()
{
    [DataField("readOnly")]
    public StampDisplayInfo DocStamp = default;
    [DataField("readOnly")]
    public string DocStampState = null!;

}

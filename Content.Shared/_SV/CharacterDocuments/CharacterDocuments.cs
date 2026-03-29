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
    public int DocID;
    public int DocType = (int)DocumentType.Employment;
    public string DocTitle = null!;
    public string DocAuthor = null!;
    public DateTime DocDateLastEdited = DateTime.Today.AddYears(200); //Today plus 200 years
    public string DocContent = "";
    public StampDisplayInfo DocStamps;
}

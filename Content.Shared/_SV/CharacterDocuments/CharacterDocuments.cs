namespace Content.Shared._SV.CharacterDocuments;

using Content.Shared.Paper;

public sealed partial class CharacterDocument
{
    public int DocID;
    public string DocTitle = null!;
    public string DocAuthor = null!;
    public DateTime DocDateLastEdited = DateTime.Today.AddYears(200); //Today plus 200 years
    public string DocContent = "";
    public StampDisplayInfo DocStamps;
}

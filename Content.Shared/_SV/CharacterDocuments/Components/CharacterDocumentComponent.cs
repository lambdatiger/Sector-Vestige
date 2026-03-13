namespace Content.Shared._SV.CharacterDocuments.Components;


public sealed partial class CharacterDocumentComponent : Component
{
    [DataField]
    public Dictionary<uint, CharacterDocument> Documents = new();

    [DataField]
    public uint SVPlayerID = 1;

    public uint GeneratePlayerID()
    {
        return SVPlayerID++;
    }
}

namespace Content.Shared._SV.CharacterDocuments.Components;

[RegisterComponent]
public sealed partial class CharacterDocumentComponent : Component
{
    [DataField]
    public Dictionary<uint, CharacterDocument> Documents = new();

    /// <summary>
    /// For now we're gonna stow this away in the characterdocuments
    /// </summary>
    [DataField]
    public uint SVPlayerID = 1;

    public uint GeneratePlayerID()
    {
        return SVPlayerID++;
    }
}

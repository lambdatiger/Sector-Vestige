namespace Content.Shared._SV.CharacterDocuments.Components;

[RegisterComponent]
public sealed partial class CharacterDocumentStationComponent : Component
{
    [DataField]
    public Dictionary<EntityUid, string> PlayerEntities = new();
}

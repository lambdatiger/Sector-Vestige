using Content.Shared.Clothing.Components;

namespace Content.Shared._SV.CharacterDocuments.Components;

[RegisterComponent]
public sealed partial class CharacterDocumentStationComponent : Component
{
    [DataField]
    public List<EntityUid> PlayerEntities = new();
}

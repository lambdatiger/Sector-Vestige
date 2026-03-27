
namespace Content.Shared._SV.CharacterDocuments.Components;

[RegisterComponent]
public sealed partial class CharacterDocumentComponent : Component
{
    [DataField]
    public Dictionary<int, CharacterDocument> Documents = new();

    public CharacterDocumentGeneral CharacterDocumentGeneral = new();

    /// <summary>
    /// Profile ID used as a stable identifier for this player's character across rounds.
    /// </summary>
    [DataField]
    public int ProfileId;

    [DataField]
    public string ProfileName;

    [DataField("readOnly")]
    public EntityUid EntityUid;
}

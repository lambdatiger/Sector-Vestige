using Robust.Shared.Serialization;

namespace Content.Shared._SV.CharacterDocuments;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class CharacterDocumentGeneral
{
    public const int TextMedLen = 64;
    public const int TextLargeLen = 4096;

    // General data about a character //
    [DataField]
    public int Height { get; private set; }
    public const int MaxHeight = 800;

    [DataField]
    public int Weight { get; private set; }
    public const int MaxWeight = 300;

    [DataField]
    public string EmergencyContactName { get; private set; }

    [DataField]
    public bool HasWorkAuthorization { get; private set; }

    // Security
    [DataField]
    public string IdentifyingFeatures { get; private set; }

    // Medical
    [DataField]
    public string Allergies { get; private set; }
    [DataField]
    public string DrugAllergies { get; private set; }
}

using Content.Shared.Security;

namespace Content.Shared._SV.CharacterDocuments.Components;

[RegisterComponent]
public sealed partial class SVCriminalRecordsComponent : Component
{
    [DataField]
    public SecurityStatus SecurityStatus = SecurityStatus.None;

    [DataField]
    public string? Reason;

    [DataField]
    public string? InitiatorName;
}

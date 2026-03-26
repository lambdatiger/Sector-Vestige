
using Content.Shared._SV.CharacterDocuments;
using Content.Shared.StationRecords;

namespace Content.Server._SV.CharacterDocuments.Consoles;

[RegisterComponent]
public sealed partial class CharacterDocumentConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public int? SelectedIndex;

    [ViewVariables(VVAccess.ReadOnly)]
    public StationRecordsFilter? Filter;

    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public DocumentType DocumentType;
}

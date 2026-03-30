
using Content.Shared._SV.CharacterDocuments;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.StationRecords;
using Robust.Shared.Audio;

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

    /// The internal scanner part ///
    /// <summary>
    /// Contains the item to be sent, assumes it's paper...
    /// </summary>
    [DataField(required: true)]
    public ItemSlot PaperSlot = new();

    /// <summary>
    /// Sprite to use when inserting an object.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string InsertingState = "inserting";

    /// <summary>
    /// How long the inserting animation will play
    /// </summary>
    [ViewVariables]
    public float InsertionTime = 1.88f; // 0.02 off for correct animation

    /// <summary>
    /// Sound to play when fax printing new message
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// Sound to play when fax successfully send message
    /// </summary>
    [DataField]
    public SoundSpecifier SendSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

}

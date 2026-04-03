
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

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public NetEntity SelectedPlayer;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public CharacterDocument? SelectedDocument;

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
    /// Remaining time of inserting animation
    /// </summary>
    [DataField]
    public float InsertingTimeRemaining;

    /// <summary>
    /// Remaining time of printing animation
    /// </summary>
    [DataField]
    public float PrintingTimeRemaining;

    /// <summary>
    /// Sound to play when we print something out
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// Sound to play when something went right like importing a document
    /// </summary>
    [DataField]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    /// <summary>
    /// Sound to play when something goes wrong
    /// </summary>
    [DataField]
    public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

}

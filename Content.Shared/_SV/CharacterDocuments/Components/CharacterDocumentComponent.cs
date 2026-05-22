
namespace Content.Shared._SV.CharacterDocuments.Components;

[RegisterComponent]
public sealed partial class CharacterDocumentComponent : Component
{
    [DataField]
    public Dictionary<int, CharacterDocument> Documents = new();

    /// <summary>
    ///     Lobby-authored flavour metadata (height, allergies, etc) for this character.
    ///     Populated from the DB on player spawn alongside <see cref="Documents"/>.
    /// </summary>
    public CharacterDocumentGeneral CharacterDocumentGeneral = new();

    /// <summary>
    /// Profile ID used as a stable identifier for this player's character across rounds.
    /// </summary>
    [DataField]
    public int ProfileId;

    [DataField]
    public string ProfileName = string.Empty;

    /// <summary>
    ///     Owning player's account username, captured at spawn. Used by the in-game
    ///     save paths so <c>SVProfile.PlayerName</c> stays correct even when the
    ///     player has since ghosted / detached from this mob (which would make
    ///     <c>TryGetSessionByEntity</c> return null and previously cause the row
    ///     to be saved as "Unknown").
    /// </summary>
    [DataField]
    public string PlayerUsername = string.Empty;
}

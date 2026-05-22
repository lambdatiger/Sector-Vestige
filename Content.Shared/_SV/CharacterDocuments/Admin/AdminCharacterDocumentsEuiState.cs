using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._SV.CharacterDocuments.Admin;

[Serializable, NetSerializable]
public sealed class AdminCharacterDocumentsEuiState : EuiStateBase
{
    public List<AdminSVProfileEntry> Profiles { get; init; } = new();
}

[Serializable, NetSerializable]
public sealed class AdminSVProfileEntry
{
    public int ProfileId;
    /// <summary>
    /// Owning player's stable account UserId. Resolved from Profile → Preference at load time.
    /// Used server-side for live-session lookup instead of <see cref="PlayerName"/> — names can
    /// drift in casing/whitespace between save and login, UserId never does.
    /// </summary>
    public Guid UserId;
    public string PlayerName = string.Empty;
    public string CharacterName = string.Empty;
    public List<CharacterDocument> Documents = new();
}

[Serializable, NetSerializable]
public sealed class AdminSVRefreshMsg : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class AdminSVDocumentEditMsg : EuiMessageBase
{
    public int ProfileId;
    public CharacterDocument Document = default!;
}

[Serializable, NetSerializable]
public sealed class AdminSVDocumentDeleteMsg : EuiMessageBase
{
    public int ProfileId;
    public int DocId;
}

[Serializable, NetSerializable]
public sealed class AdminSVDocumentCreateMsg : EuiMessageBase
{
    public int ProfileId;
    public int DocType;
    public string Title = string.Empty;
    public string Content = string.Empty;
    public List<CharacterDocumentStamp> Stamps = new();
}

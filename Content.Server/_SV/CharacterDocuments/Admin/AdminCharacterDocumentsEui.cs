using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.Preferences.Managers;
using Content.Shared._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments.Admin;
using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.Eui;
using Robust.Server.Player;

namespace Content.Server._SV.CharacterDocuments.Admin;

public sealed partial class AdminCharacterDocumentsEui : BaseEui
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;

    private List<AdminSVProfileEntry> _profiles = new();

    public AdminCharacterDocumentsEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override EuiStateBase GetNewState()
    {
        return new AdminCharacterDocumentsEuiState { Profiles = _profiles };
    }

    public override void Opened()
    {
        base.Opened();
        _ = ReloadAsync();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case AdminSVRefreshMsg:
                _ = ReloadAsync();
                break;
            case AdminSVDocumentEditMsg edit:
                _ = ApplyEditAsync(edit.ProfileId, edit.Document);
                break;
            case AdminSVDocumentDeleteMsg del:
                _ = ApplyDeleteAsync(del.ProfileId, del.DocId);
                break;
            case AdminSVDocumentCreateMsg create:
                _ = ApplyCreateAsync(create);
                break;
        }
    }

    private async Task ReloadAsync()
    {
        var dbProfiles = await _db.GetAllSVCharacterDocumentsAsync(CancellationToken.None);

        _profiles = dbProfiles
            .OrderBy(p => p.PlayerName)
            .ThenBy(p => p.CharacterName)
            .Select(p => new AdminSVProfileEntry
            {
                ProfileId = p.ProfileId,
                UserId = p.Profile?.Preference?.UserId ?? Guid.Empty,
                PlayerName = p.PlayerName,
                CharacterName = p.CharacterName,
                Documents = p.CharacterDocuments
                    .Select(d => new CharacterDocument
                    {
                        DocID = d.DocID,
                        DocType = d.DocType,
                        DocTitle = d.DocTitle,
                        DocAuthor = d.DocAuthor,
                        DocLastEditedBy = d.DocLastEditedBy,
                        DocDateLastEdited = d.DocDateLastEdited,
                        DocContent = d.DocContent,
                        DocStamps = CharacterDocumentDeserializer.DeserializeStamps(d.DocStamps),
                    })
                    .OrderBy(d => d.DocTitle)
                    .ToList(),
            })
            .ToList();

        StateDirty();
    }

    private const int TitleMaxLen = 256;
    private const int ContentMaxLen = 8192;

    private async Task ApplyEditAsync(int profileId, CharacterDocument incoming)
    {
        var entry = _profiles.FirstOrDefault(p => p.ProfileId == profileId);
        if (entry == null)
            return;

        var idx = entry.Documents.FindIndex(d => d.DocID == incoming.DocID);
        if (idx < 0)
            return;

        // Mutate in-memory only as a scratchpad; the canonical state comes from PersistAsync's reload.
        var existing = entry.Documents[idx];
        existing.DocTitle = Clamp(incoming.DocTitle, TitleMaxLen);
        existing.DocContent = Clamp(incoming.DocContent, ContentMaxLen);
        existing.DocStamps = incoming.DocStamps ?? new List<CharacterDocumentStamp>();
        existing.DocLastEditedBy = "Central Command";
        existing.DocDateLastEdited = DateTime.Now.AddYears(200);

        await PersistAsync(profileId, entry);
    }

    private async Task ApplyDeleteAsync(int profileId, int docId)
    {
        var entry = _profiles.FirstOrDefault(p => p.ProfileId == profileId);
        if (entry == null)
            return;

        entry.Documents.RemoveAll(d => d.DocID == docId);

        await PersistAsync(profileId, entry);
    }

    private async Task ApplyCreateAsync(AdminSVDocumentCreateMsg msg)
    {
        var entry = _profiles.FirstOrDefault(p => p.ProfileId == msg.ProfileId);
        if (entry == null)
            return;

        // Reject unknown document types from a malicious or stale client.
        if (!Enum.IsDefined(typeof(DocumentType), msg.DocType))
            return;

        // The DocID here is throwaway — the DB autoincrements on insert, and the
        // canonical id arrives via PersistAsync's reload before anyone sees state.
        var throwawayId = entry.Documents.Count == 0 ? 1 : entry.Documents.Max(d => d.DocID) + 1;

        entry.Documents.Add(new CharacterDocument
        {
            DocID = throwawayId,
            DocType = msg.DocType,
            DocTitle = Clamp(string.IsNullOrWhiteSpace(msg.Title) ? "Untitled" : msg.Title, TitleMaxLen),
            DocAuthor = "Central Command",
            DocLastEditedBy = "Central Command",
            DocDateLastEdited = DateTime.Now.AddYears(200),
            DocContent = Clamp(msg.Content ?? string.Empty, ContentMaxLen),
            DocStamps = msg.Stamps ?? new List<CharacterDocumentStamp>(),
        });

        await PersistAsync(msg.ProfileId, entry);
    }

    /// <summary>
    ///     Single canonical save path. Persists the entry to the DB, reloads the
    ///     admin cache so canonical autoincrement DocIDs replace any throwaway
    ///     in-memory ones, then syncs the live player session with the canonical
    ///     state and finally pushes state to admin clients. Nobody — neither admin
    ///     UI nor live console — ever sees the throwaway IDs.
    /// </summary>
    private async Task PersistAsync(int profileId, AdminSVProfileEntry entry)
    {
        var dbDocs = entry.Documents.Select(d => new SVModel.CharacterDocument
        {
            DocTitle = d.DocTitle,
            DocAuthor = d.DocAuthor,
            DocLastEditedBy = d.DocLastEditedBy,
            DocContent = d.DocContent,
            DocDateLastEdited = d.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(d.DocStamps),
            DocType = d.DocType,
            ProfileId = entry.ProfileId,
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync(entry.ProfileId, entry.PlayerName, entry.CharacterName, dbDocs);

        await ReloadAsync();

        var fresh = _profiles.FirstOrDefault(p => p.ProfileId == profileId);
        if (fresh != null)
            SyncLiveSession(fresh);

        // Push fresh preferences to the affected player (if connected) so their
        // lobby tab + customization menu reflect the admin edit immediately.
        // Prefer the reloaded entry's UserId — `entry` here is the pre-reload
        // copy and may have an empty UserId on rows that pre-date this fix.
        await RefreshPlayerPrefsAsync(fresh ?? entry);
    }

    /// <summary>
    ///     Pushes fresh preferences to the affected player so their open lobby UI
    ///     re-renders the new docs. Looks up the session by UserId rather than by name —
    ///     names can drift in casing or whitespace between save and login (especially
    ///     for OAuth-style usernames), UserId never does.
    /// </summary>
    private async Task RefreshPlayerPrefsAsync(AdminSVProfileEntry entry)
    {
        if (entry.UserId == Guid.Empty)
            return;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.UserId.UserId == entry.UserId)
            {
                await _prefs.RefreshPreferencesForUserAsync(session);
                return;
            }
        }
    }

    private static string Clamp(string? s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length > maxLen ? s[..maxLen] : s;
    }

    /// <summary>
    ///     If the target character is currently spawned, replace their in-memory
    ///     document dict and broadcast a refresh so any open consoles re-render.
    ///     Without this, admins editing online players' docs would only show up
    ///     after the player reconnects.
    /// </summary>
    private void SyncLiveSession(AdminSVProfileEntry entry)
    {
        var query = _entMan.EntityQueryEnumerator<CharacterDocumentComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ProfileId != entry.ProfileId)
                continue;

            comp.Documents.Clear();
            foreach (var doc in entry.Documents)
                comp.Documents[doc.DocID] = doc;

            _entMan.EventBus.RaiseEvent(EventSource.Local, new CharacterDocumentEditedEvent());
            return; // at most one live entity per profile
        }
    }
}

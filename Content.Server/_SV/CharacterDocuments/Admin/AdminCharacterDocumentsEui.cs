using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments.Admin;
using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.Eui;

namespace Content.Server._SV.CharacterDocuments.Admin;

public sealed class AdminCharacterDocumentsEui : BaseEui
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

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

    private async Task ApplyEditAsync(int profileId, CharacterDocument incoming)
    {
        var entry = _profiles.FirstOrDefault(p => p.ProfileId == profileId);
        if (entry == null)
            return;

        var idx = entry.Documents.FindIndex(d => d.DocID == incoming.DocID);
        if (idx < 0)
            return;

        // preserve original author + ID, only change editable fields
        var existing = entry.Documents[idx];
        existing.DocTitle = incoming.DocTitle;
        existing.DocContent = incoming.DocContent;
        existing.DocStamps = incoming.DocStamps;
        existing.DocLastEditedBy = "Central Command";
        existing.DocDateLastEdited = DateTime.Now.AddYears(200);

        await SaveProfileAsync(entry);
        StateDirty();
    }

    private async Task ApplyDeleteAsync(int profileId, int docId)
    {
        var entry = _profiles.FirstOrDefault(p => p.ProfileId == profileId);
        if (entry == null)
            return;

        entry.Documents.RemoveAll(d => d.DocID == docId);

        await SaveProfileAsync(entry);
        StateDirty();
    }

    private async Task ApplyCreateAsync(AdminSVDocumentCreateMsg msg)
    {
        var entry = _profiles.FirstOrDefault(p => p.ProfileId == msg.ProfileId);
        if (entry == null)
            return;

        // Generate a fresh DocID — must not collide with existing ones.
        // The DB autoincrements the actual primary key on insert; this in-memory ID
        // is only used by the client UI for selection until the next ReloadAsync.
        var newId = entry.Documents.Count == 0 ? 1 : entry.Documents.Max(d => d.DocID) + 1;

        var doc = new CharacterDocument
        {
            DocID = newId,
            DocType = msg.DocType,
            DocTitle = string.IsNullOrWhiteSpace(msg.Title) ? "Untitled" : msg.Title,
            DocAuthor = "Central Command",
            DocLastEditedBy = "Central Command",
            DocDateLastEdited = DateTime.Now.AddYears(200),
            DocContent = msg.Content,
            DocStamps = msg.Stamps ?? new List<CharacterDocumentStamp>(),
        };
        entry.Documents.Add(doc);

        await SaveProfileAsync(entry);
        // Pick up the real DocID assigned by the DB.
        await ReloadAsync();
    }

    private async Task SaveProfileAsync(AdminSVProfileEntry entry)
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

        SyncLiveSession(entry);
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

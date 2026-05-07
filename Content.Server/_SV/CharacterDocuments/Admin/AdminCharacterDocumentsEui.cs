using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments.Admin;
using Content.Shared.Eui;

namespace Content.Server._SV.CharacterDocuments.Admin;

public sealed class AdminCharacterDocumentsEui : BaseEui
{
    [Dependency] private readonly IServerDbManager _db = default!;

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
    }
}

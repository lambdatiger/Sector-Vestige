using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.StationRecords.Systems;
using Content.Server._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.StationRecords;
using Robust.Shared.Player;
using Robust.Server.Player;
using Robust.Shared.Network;
using Content.Server._SV.CharacterDocuments.Consoles;
using Content.Shared._SV.CharacterDocuments.Consoles;
using Content.Server.CriminalRecords.Systems;
using Content.Server.Station.Systems;

namespace Content.Server._SV.CharacterDocuments;

public sealed partial class CharacterDocumentSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn, after: [typeof(StationRecordsSystem)]);

    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<StationRecordsComponent>(args.Station))
        {
            Log.Error("Station does not have StationRecordsComponent PlayerDocs will not work");
            return;
        }

        if (!HasComp<CharacterDocumentStationComponent>(args.Station))
        {
            AddComp<CharacterDocumentStationComponent>(args.Station);
        }

        var player = args.Mob;
        if (!HasComp<CharacterDocumentComponent>(player) && HasComp<HumanoidProfileComponent>(player))
        {
            AddComp<CharacterDocumentComponent>(player);
            var comp = Comp<CharacterDocumentComponent>(player);
            comp.ProfileName = args.Profile.Name;
        }

        _ = LoadPlayerDocumentsAsync(player, args.Profile.Name);
    }

    private async Task LoadPlayerDocumentsAsync(EntityUid uid, string characterName, bool notifyUI = false)
    {
        _playerManager.TryGetSessionByEntity(uid, out var session);
        if (session == null) return;

        var netUserId = session.UserId;
        var playerName = session.Name;

        var prefs = await _db.GetPlayerPreferencesAsync(netUserId, CancellationToken.None);
        if (prefs == null)
        {
            Log.Error($"Could not load preferences for player {playerName} ({characterName})");
            return;
        }

        var profile = prefs.Profiles.FirstOrDefault(p => p.CharacterName == characterName);
        if (profile == null)
        {
            Log.Error($"Could not find profile '{characterName}' for player {playerName}");
            return;
        }

        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        docComp.ProfileId = profile.Id;
        var result = await _db.GetSVCharacterDocumentsAsync(profile.Id);
        if (result == null)
            return;

        docComp.Documents.Clear();
        foreach (var doc in result.Value.Documents)
        {
            var characterDoc = new CharacterDocument
            {
                DocID = doc.DocID,
                DocType = doc.DocType,
                DocTitle = doc.DocTitle,
                DocAuthor = doc.DocAuthor,
                DocDateLastEdited = doc.DocDateLastEdited,
                DocContent = doc.DocContent,
                DocStamps = CharacterDocumentDeserializer.DeserializeStamps(doc.DocStamps)
            };
            docComp.Documents[doc.DocID] = characterDoc;
        }

        if (notifyUI)
        {
            RaiseLocalEvent(new CharacterDocumentEditedEvent());
        }
    }

    public async Task AddDocument(EntityUid uid, CharacterDocument characterDocument)
    {
        _playerManager.TryGetSessionByEntity(uid, out var session);
        var playerName = session?.Name ?? "Unknown";

        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        var id = docComp.Documents.Count == 0 ? 1 : docComp.Documents.Keys.Max() + 1;
        characterDocument.DocID = id;
        docComp.Documents.Add(id, characterDocument);

        var dbDocs = docComp.Documents.Values.Select(doc => new SVModel.CharacterDocument
        {
            DocTitle = doc.DocTitle,
            DocAuthor = doc.DocAuthor,
            DocContent = doc.DocContent,
            DocDateLastEdited = doc.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(doc.DocStamps),
            DocType = doc.DocType,
            ProfileId = docComp.ProfileId
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync(docComp.ProfileId, playerName, docComp.ProfileName, dbDocs);
        RaiseLocalEvent(new CharacterDocumentEditedEvent());
    }

    public async Task DeleteDocument(EntityUid uid, CharacterDocument characterDocument)
    {
        _playerManager.TryGetSessionByEntity(uid, out var session);
        var playerName = session?.Name ?? "Unknown";

        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        docComp.Documents.Remove(characterDocument.DocID);

        var dbDocs = docComp.Documents.Values.Select(doc => new SVModel.CharacterDocument
        {
            DocTitle = doc.DocTitle,
            DocAuthor = doc.DocAuthor,
            DocContent = doc.DocContent,
            DocDateLastEdited = doc.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(doc.DocStamps),
            DocType = doc.DocType,
            ProfileId = docComp.ProfileId
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync(docComp.ProfileId, playerName, docComp.ProfileName, dbDocs);
        RaiseLocalEvent(new CharacterDocumentEditedEvent());
    }

    public async Task UpdateDocument(EntityUid uid, CharacterDocument characterDocument)
    {
        _playerManager.TryGetSessionByEntity(uid, out var session);
        var playerName = session?.Name ?? "Unknown";

        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        docComp.Documents[characterDocument.DocID] = characterDocument;

        var dbDocs = docComp.Documents.Values.Select(doc => new SVModel.CharacterDocument
        {
            DocTitle = doc.DocTitle,
            DocAuthor = doc.DocAuthor,
            DocContent = doc.DocContent,
            DocDateLastEdited = doc.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(doc.DocStamps),
            DocType = doc.DocType,
            ProfileId = docComp.ProfileId
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync(docComp.ProfileId, playerName, docComp.ProfileName, dbDocs);
        RaiseLocalEvent(new CharacterDocumentEditedEvent());
    }
}
public sealed class CharacterDocumentEditedEvent : EntityEventArgs;

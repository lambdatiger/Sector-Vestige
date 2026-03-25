using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.StationRecords.Systems;
using Content.Server._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Players;
using Content.Shared.StationRecords;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._SV.CharacterDocuments;

public sealed partial class CharacterDocumentSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;

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
        }

        _ = LoadPlayerDocumentsAsync(player, args.Player, args.Profile.Name);
    }

    private async Task LoadPlayerDocumentsAsync(EntityUid mob, ICommonSession player, string characterName)
    {
        var prefs = await _db.GetPlayerPreferencesAsync(player.UserId, CancellationToken.None);
        if (prefs == null)
        {
            Log.Error($"Could not load preferences for player {player.Name}");
            return;
        }

        var profile = prefs.Profiles.FirstOrDefault(p => p.CharacterName == characterName);
        if (profile == null)
        {
            Log.Error($"Could not find profile '{characterName}' for player {player.Name}");
            return;
        }

        if (!TryComp<CharacterDocumentComponent>(mob, out var docComp))
            return;

        var result = await _db.GetSVCharacterDocumentsAsync(profile.Id);

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
    }

    private async Task AddDocument(EntityUid mob, ICommonSession player, CharacterDocument characterDocument)
    {
        if (!TryComp<CharacterDocumentComponent>(mob, out var docComp))
            return;

        var id = docComp.Documents.Count == 0 ? 1 : docComp.Documents.Keys.Max() + 1;
        docComp.Documents.Add(id, characterDocument);

        var dbDocs = docComp.Documents.Values.Select(doc => new SVModel.CharacterDocument
        {
            DocTitle = doc.DocTitle,
            DocAuthor = doc.DocAuthor,
            DocContent = doc.DocContent,
            DocDateLastEdited = doc.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(doc.DocStamps),
            DocType = doc.DocType,
            SVProfileID = (int)docComp.SVPlayerID
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync((int)docComp.SVPlayerID, player.Name, characterDocument.DocTitle, CharacterDocumentSerializer.SerializeDocument(dbDocs), dbDocs);
    }

    private async Task DeleteDocument(EntityUid mob, ICommonSession player, CharacterDocument characterDocument)
    {
        if (!TryComp<CharacterDocumentComponent>(mob, out var docComp))
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
            SVProfileID = (int)docComp.SVPlayerID
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync((int)docComp.SVPlayerID, player.Name, characterDocument.DocTitle, CharacterDocumentSerializer.SerializeDocument(dbDocs), dbDocs);
    }
}

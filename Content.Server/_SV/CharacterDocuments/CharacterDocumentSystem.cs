using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.StationRecords.Systems;
using Content.Shared._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Players;
using Content.Shared.StationRecords;
using Robust.Shared.Player;

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

        docComp.SVPlayerID = (uint) profile.Id;

        var result = await _db.GetSVCharacterDocumentsAsync(profile.Id);
        if (result == null)
        {
            await _db.SaveSVCharacterDocumentsAsync(profile.Id, player.Name, characterName, JsonDocument.Parse("{}"), []);
            return;
        }

        foreach (var doc in result.Value.Documents)
        {
            var characterDoc = new CharacterDocument
            {
                DocID = doc.DocID,
                DocTitle = doc.DocTitle,
                DocAuthor = doc.DocAuthor,
                DocDateLastEdited = doc.DocDateLastEdited,
                DocContent = doc.DocContent,
                // TODO: deserialize doc.DocStamps (string) into DocStamp (StampType enum)
            };
            docComp.Documents[(uint) doc.DocID] = characterDoc;
        }
    }

    private void AddDocument(CharacterDocument characterDocument)
    {

    }
}

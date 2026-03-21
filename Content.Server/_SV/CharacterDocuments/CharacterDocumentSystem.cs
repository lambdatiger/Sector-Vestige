using Content.Server.StationRecords.Systems;
using Content.Shared._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.StationRecords;

namespace Content.Server._SV.CharacterDocuments;

public sealed partial class CharacterDocumentSystem : EntitySystem
{
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
    }

    private void AddDocuments(CharacterDocument characterDocument)
    {

    }
}

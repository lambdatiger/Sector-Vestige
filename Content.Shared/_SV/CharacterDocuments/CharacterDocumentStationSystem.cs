
using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.GameTicking;

namespace Content.Shared._SV.CharacterDocuments;

public sealed partial class CharacterDocumentStationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    public void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<CharacterDocumentStationComponent>(args.Station))
            Log.Error("Station did not have the CharacterDocumentStationComponent");
        return;
    }
}

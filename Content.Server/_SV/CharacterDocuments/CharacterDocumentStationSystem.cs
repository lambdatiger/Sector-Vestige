using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.GameTicking;
using Content.Shared.Station;

namespace Content.Server._SV.CharacterDocuments;

public sealed partial class CharacterDocumentStationSystem : EntitySystem
{
    [Dependency] private readonly SharedStationSystem _sharedStationSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn, after: [typeof(CharacterDocumentSystem)]);
        SubscribeLocalEvent<CharacterDocumentComponent, EntParentChangedMessage>(OnParentChanged);

    }

    public void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<CharacterDocumentComponent>(args.Mob, out var playercomp))
            return;

        if (!TryComp<CharacterDocumentStationComponent>(args.Station, out var stationComp))
            return;

        if (!stationComp.PlayerEntities.ContainsKey(args.Mob))
        {
            stationComp.PlayerEntities.Add(args.Mob, playercomp.ProfileName);
            Log.Debug($"Added {playercomp.ProfileName} to the list of {args.Station} it now has a count of {stationComp.PlayerEntities.Count}");
        }
    }

    public void OnParentChanged(EntityUid uid, CharacterDocumentComponent component, ref EntParentChangedMessage args)
    {
        var stations = _sharedStationSystem.GetStations();
        foreach (EntityUid station in stations)
        {
            if (!TryComp<CharacterDocumentStationComponent>(station, out var stationComp))
                continue;

            if (!stationComp.PlayerEntities.ContainsKey(uid))
            {
                stationComp.PlayerEntities.Add(uid, component.ProfileName);
                Log.Debug($"Added {component.ProfileName} to the list of {station.Id} it now has a count of {stationComp.PlayerEntities.Count}");
            }
        }
    }
}

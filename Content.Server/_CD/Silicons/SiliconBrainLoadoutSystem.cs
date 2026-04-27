using System.Diagnostics;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.Silicons.Laws;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CD.Silicons;

[Virtual]
public partial class SharedSiliconBrainLoadout : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStationSpawningSystem _station = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly PlayerSystem _player = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedStationAiSystem _ai = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SiliconLawSystem _laws = default!;

    private ProtoId<LoadoutGroupPrototype> CyborgBrainLoadoutPrototype => "CyborgBrain";
    private ProtoId<LoadoutGroupPrototype> StationAiLawsetLoadoutPrototype => "StationAiLawset";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, CdPlayerSpawnBeforeMindEvent>(OnBorgPlayerSpawnComplete);
        SubscribeLocalEvent<StationAiHeldComponent, CdPlayerSpawnBeforeMindEvent>(OnAIPlayerSpawnComplete);
    }

    protected void OnAIPlayerSpawnComplete(Entity<StationAiHeldComponent> ent, ref CdPlayerSpawnBeforeMindEvent args)
    {
        // surely there's a better way of doing this
        var jobLoadoutId = LoadoutSystem.GetJobPrototype(args.JobId);
        var selectedLoadouts = args
            .Character
            .GetLoadoutOrDefault(jobLoadoutId, args.Player, args.Character.Species, EntityManager, _proto)
            .SelectedLoadouts[StationAiLawsetLoadoutPrototype];

        Debug.Assert(selectedLoadouts.Count == 1);
        var loadoutProto = selectedLoadouts[0].Prototype;

        if(!_proto.TryIndex(loadoutProto, out var loadout) ||
           loadout.Lawset is not {} laws)
            return;

        var lawset = _laws.GetLawset(laws);
        _laws.SetLaws(lawset.Laws, ent);
    }


    private void OnBorgPlayerSpawnComplete(Entity<BorgChassisComponent> ent, ref CdPlayerSpawnBeforeMindEvent args)
    {
        var jobLoadoutId = LoadoutSystem.GetJobPrototype(args.JobId);
        var selectedLoadouts = args
            .Character
            .GetLoadoutOrDefault(jobLoadoutId, args.Player, args.Character.Species, EntityManager, _proto)
            .SelectedLoadouts[CyborgBrainLoadoutPrototype];

        Debug.Assert(selectedLoadouts.Count == 1);
        var loadoutProto = selectedLoadouts[0].Prototype;

        if (!_container.TryGetContainer(ent, ent.Comp.BrainContainerId, out var container) ||
            !_proto.TryIndex(loadoutProto, out var loadout))
            return;

        // only run if we're absolutely SURE that we have a proto to replace it with
        foreach (var brain in _container.EmptyContainer(container))
        {
            QueueDel(brain);
        }

        TrySpawnInContainer(loadout.Brain, ent, ent.Comp.BrainContainerId, out var mmi);
    }
}

/// <summary>
/// Event that raises right before the mind is added to the player entity while spawning on station.
/// </summary>
public sealed record CdPlayerSpawnBeforeMindEvent(ICommonSession Player, HumanoidCharacterProfile Character, ProtoId<JobPrototype> JobId);

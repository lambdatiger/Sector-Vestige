using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared._SV.CharacterDocuments.Consoles;
using Content.Server._SV.CharacterDocuments.Consoles;
using Content.Server._SV.CharacterDocuments;
using Content.Shared.GameTicking;
using Content.Shared.Station;
using Robust.Server.GameObjects;
using System.Reflection.Metadata;
using Content.Shared.Materials.OreSilo;

namespace Content.Server._SV.CharacterDocuments.Consoles;

public sealed class CharacterDocumentConsoleSystem : EntitySystem
{

    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedStationSystem _sharedStationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterDocumentEditedEvent>(OnDocumentEdited);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, SelectCharacterDocumentPlayer>(OnSelectedPlayer);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, SelectCharacterDocument>(OnSelectedDocument);

    }

    public void OnDocumentEdited(CharacterDocumentEditedEvent args)
    {
        var query = EntityQueryEnumerator<CharacterDocumentConsoleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var station = _sharedStationSystem.GetOwningStation(uid);
            if (station == null) continue;

            if (!TryComp<CharacterDocumentStationComponent>(station, out var stationComponent))
                continue;

            var netPlayerEntities = new Dictionary<NetEntity, string>();
            foreach (var (playerUid, name) in stationComponent.PlayerEntities)
                netPlayerEntities.Add(GetNetEntity(playerUid), name);

            var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null);
            _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, characterDocumentConsoleState);
        }

    }

    public void OnBuiOpened(EntityUid uid, CharacterDocumentConsoleComponent comp, BoundUIOpenedEvent args)
    {
        var station = _sharedStationSystem.GetOwningStation(uid);
        if (station == null) return;

        if (!TryComp<CharacterDocumentStationComponent>(station, out var stationComponent))
            return;

        var netPlayerEntities = new Dictionary<NetEntity, string>();
        foreach (var (playerUid, name) in stationComponent.PlayerEntities)
            netPlayerEntities.Add(GetNetEntity(playerUid), name);

        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null);
        _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, characterDocumentConsoleState);
    }

    public void OnSelectedPlayer(EntityUid uid, CharacterDocumentConsoleComponent comp, SelectCharacterDocumentPlayer args)
    {
        var station = _sharedStationSystem.GetOwningStation(uid);
        if (station == null) return;

        if (!TryComp<CharacterDocumentStationComponent>(station, out var stationComponent))
            return;

        var player = GetEntity(args.Player);
        if (!TryComp<CharacterDocumentComponent>(player, out var documentComponent))
            return;

        var netPlayerEntities = new Dictionary<NetEntity, string>();
        foreach (var (playerUid, name) in stationComponent.PlayerEntities)
            netPlayerEntities.Add(GetNetEntity(playerUid), name);

        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, documentComponent.Documents, null);
        _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, characterDocumentConsoleState);
    }

    public void OnSelectedDocument(EntityUid uid, CharacterDocumentConsoleComponent comp, SelectCharacterDocument args)
    {
        var station = _sharedStationSystem.GetOwningStation(uid);
        if (station == null) return;

        if (!TryComp<CharacterDocumentStationComponent>(station, out var stationComponent))
            return;

        var player = GetEntity(args.Player);
        if (!TryComp<CharacterDocumentComponent>(player, out var documentComponent))
            return;

        documentComponent.Documents.TryGetValue(args.DocID, out var selecteddoc);

        var netPlayerEntities = new Dictionary<NetEntity, string>();
        foreach (var (playerUid, name) in stationComponent.PlayerEntities)
            netPlayerEntities.Add(GetNetEntity(playerUid), name);

        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, documentComponent.Documents, selecteddoc);
        _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, characterDocumentConsoleState);
    }

}

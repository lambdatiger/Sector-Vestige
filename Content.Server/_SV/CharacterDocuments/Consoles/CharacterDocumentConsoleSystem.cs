using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared._SV.CharacterDocuments.Consoles;
using Content.Server._SV.CharacterDocuments.Consoles;
using Content.Server._SV.CharacterDocuments;
using Content.Shared.GameTicking;
using Content.Shared.Station;
using Robust.Server.GameObjects;
using Content.Shared.Paper;
using Content.Shared._SV.CharacterDocuments;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using System.Linq;
using Robust.Server.Player;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using NetCord;
using Content.Server.Popups;
using Content.Shared.Coordinates;
using Robust.Shared.Utility;
using Content.Shared.Fax.Components;
using System.Threading.Tasks;

namespace Content.Server._SV.CharacterDocuments.Consoles;

public sealed class CharacterDocumentConsoleSystem : EntitySystem
{

    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedStationSystem _sharedStationSystem = default!;
    [Dependency] private readonly CharacterDocumentSystem _characterDocumentSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterDocumentConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<CharacterDocumentEditedEvent>(OnDocumentEdited);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, BoundUIClosedEvent>(OnBuiClosed);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, SelectCharacterDocumentPlayer>(OnSelectedPlayer);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, SelectCharacterDocument>(OnSelectedDocument);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentScan>(OnDocumentScanned);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentDelete>(OnDocumentDelete);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentDeselect>(OnDocumentDeselected);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentPrint>(OnDocumentPrint);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentEdit>(OnDocumentEdit);

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

            var player = GetEntity(comp.SelectedPlayer);
            if (TryComp<CharacterDocumentComponent>(player, out var docComp))
            {
                bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
                var state = new CharacterDocumentConsoleState(netPlayerEntities, comp.SelectedPlayer, docComp.Documents, null, paperinserted);
                _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, state);
            }
            else
            {
                bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
                var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null, paperinserted);
                _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, characterDocumentConsoleState);
            }
        }

    }

    public void OnConsoleInit(EntityUid uid, CharacterDocumentConsoleComponent comp, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, "Paper", comp.PaperSlot);
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

        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null || false;
        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null, paperinserted);
        _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, characterDocumentConsoleState);
    }

    public void OnBuiClosed(EntityUid uid, CharacterDocumentConsoleComponent comp, BoundUIClosedEvent args)
    {
        comp.SelectedPlayer = default;
        comp.SelectedDocument = null;

        var station = _sharedStationSystem.GetOwningStation(uid);
        if (station == null) return;

        if (!TryComp<CharacterDocumentStationComponent>(station, out var stationComponent))
            return;

        var netPlayerEntities = new Dictionary<NetEntity, string>();
        foreach (var (playerUid, name) in stationComponent.PlayerEntities)
            netPlayerEntities.Add(GetNetEntity(playerUid), name);

        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null || false;
        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null, paperinserted);
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

        comp.SelectedPlayer = args.Player;
        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, documentComponent.Documents, null, paperinserted);
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
        comp.SelectedDocument = selecteddoc;

        var netPlayerEntities = new Dictionary<NetEntity, string>();
        foreach (var (playerUid, name) in stationComponent.PlayerEntities)
            netPlayerEntities.Add(GetNetEntity(playerUid), name);

        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, documentComponent.Documents, selecteddoc, paperinserted);
        _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, characterDocumentConsoleState);
    }

    public async void OnDocumentScanned(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentScan args)
    {
        var paper = comp.PaperSlot.ContainerSlot?.ContainedEntity;
        var player = GetEntity(args.Player);

        if (!TryComp<PaperComponent>(paper, out var paperComponent))
            return;

        if (string.IsNullOrWhiteSpace(args.DocTitle))
        {
            _audio.PlayPvs(comp.ErrorSound, uid);
            return;
        }

        var stamps = new List<CharacterDocumentStamp>();
        if (paperComponent.StampedBy != null && paperComponent.StampState != null)
        {
            foreach (StampDisplayInfo stamp in paperComponent.StampedBy)
            {
                stamps.Add(new CharacterDocumentStamp { DocStamp = stamp, DocStampState = paperComponent.StampState });
            }
        }

        var providedDocument = new CharacterDocument()
        {
            DocTitle = args.DocTitle,
            DocAuthor = "Testvalue_Author",
            DocContent = paperComponent.Content,
            DocType = (int)comp.DocumentType,
            DocStamps = stamps
        };

        _audio.PlayPvs(comp.SuccessSound, uid);
        await _characterDocumentSystem.AddDocument(player, providedDocument);
    }

    public async void OnDocumentDelete(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentDelete args)
    {
        var player = GetEntity(args.Player);
        if (args.CharacterDocument == null)
        {
            _audio.PlayPvs(comp.ErrorSound, uid);
            return;
        }

        _audio.PlayPvs(comp.SuccessSound, uid);
        await _characterDocumentSystem.DeleteDocument(player, args.CharacterDocument);
    }

    public void OnDocumentPrint(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentPrint args)
    {
        var printed = Spawn("SVPaperDocumentation", uid.ToCoordinates());

        var stamps = new List<StampDisplayInfo>();
        string stampState = string.Empty;
        if (comp.SelectedDocument?.DocStamps is { Count: > 0 } docStamps)
        {
            stampState = docStamps[0].DocStampState;
            foreach (var stamp in docStamps)
                stamps.Add(stamp.DocStamp);
        }

        if (!TryComp<PaperComponent>(printed, out var paperComponent))
            return;
        else
        {
            _metaData.SetEntityName(printed, comp.SelectedDocument?.DocTitle ?? "Error in Printing, please report to NT R&D");
            _paperSystem.SetContent(printed, comp.SelectedDocument?.DocContent ?? "Error in Printing, please report to NT R&D");
            foreach (StampDisplayInfo stamp in stamps)
            {
                _paperSystem.TryStamp((printed, paperComponent), stamp, stampState);
            }
        }
        _audio.PlayPvs(comp.PrintSound, uid);
    }

    public async void OnDocumentEdit(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentEdit args)
    {
        if (comp.SelectedDocument == null)
            return;

        if (args.CharacterDocument == null)
            return;

        var player = GetEntity(args.Player);

        CharacterDocument oldCharacterDoc = comp.SelectedDocument;

        var newCharacterDoc = new CharacterDocument()
        {
            DocID = oldCharacterDoc.DocID,
            DocTitle = args.CharacterDocument.DocTitle,
            DocAuthor = args.CharacterDocument.DocAuthor,
            DocContent = args.CharacterDocument.DocContent,
            DocDateLastEdited = DateTime.Now.AddYears(200),
            DocStamps = args.CharacterDocument.DocStamps,
            DocType = args.CharacterDocument.DocType
        };
        _audio.PlayPvs(comp.SuccessSound, uid);

        var station = _sharedStationSystem.GetOwningStation(uid);
        if (station == null) return;

        if (!TryComp<CharacterDocumentStationComponent>(station, out var stationComponent))
            return;

        if (!TryComp<CharacterDocumentComponent>(player, out var documentComponent))
            return;

        var netPlayerEntities = new Dictionary<NetEntity, string>();
        foreach (var (playerUid, name) in stationComponent.PlayerEntities)
            netPlayerEntities.Add(GetNetEntity(playerUid), name);

        await _characterDocumentSystem.UpdateDocument(player, newCharacterDoc);
        comp.SelectedDocument = documentComponent.Documents.TryGetValue(newCharacterDoc.DocID, out var reloadedDoc)
            ? reloadedDoc
            : newCharacterDoc;

        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, documentComponent.Documents, newCharacterDoc, paperinserted);
        _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, characterDocumentConsoleState);
    }

    public void OnDocumentDeselected(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentDeselect args)
    {
        comp.SelectedDocument = null;
        RefreshSlotState(uid, comp);
    }

    public void OnSlotChanged(EntityUid uid, CharacterDocumentConsoleComponent comp, ref EntInsertedIntoContainerMessage args)
        => RefreshSlotState(uid, comp);

    public void OnSlotChanged(EntityUid uid, CharacterDocumentConsoleComponent comp, ref EntRemovedFromContainerMessage args)
        => RefreshSlotState(uid, comp);

    private void RefreshSlotState(EntityUid uid, CharacterDocumentConsoleComponent comp)
    {
        var station = _sharedStationSystem.GetOwningStation(uid);
        if (station == null) return;

        if (!TryComp<CharacterDocumentStationComponent>(station, out var stationComponent))
            return;

        var netPlayerEntities = new Dictionary<NetEntity, string>();

        foreach (var (playerUid, name) in stationComponent.PlayerEntities)
            netPlayerEntities.Add(GetNetEntity(playerUid), name);

        var isPaperInserted = comp.PaperSlot.Item.HasValue;
        if (isPaperInserted)
        {
            comp.InsertingTimeRemaining = comp.InsertionTime;
            _itemSlotsSystem.SetLock(uid, comp.PaperSlot, false);
        }

        var player = GetEntity(comp.SelectedPlayer);

        if (TryComp<CharacterDocumentComponent>(player, out var docComp))
        {
            var state = new CharacterDocumentConsoleState(netPlayerEntities, comp.SelectedPlayer, docComp.Documents, comp.SelectedDocument, isPaperInserted);
            _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, state);
        }
        else
        {
            var state = new CharacterDocumentConsoleState(netPlayerEntities, null, null, comp.SelectedDocument, isPaperInserted);
            _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, state);
        }
    }

    // private void ProcessInsertingAnimation(EntityUid uid, float frameTime, CharacterDocumentConsoleComponent comp)
    // {
    //     if (comp.InsertingTimeRemaining <= 0)
    //         return;

    //     comp.InsertingTimeRemaining -= frameTime;
    //     UpdateAppearance(uid, comp);
    // }

    // private void UpdateAppearance(EntityUid uid, CharacterDocumentConsoleComponent? component = null)
    // {
    //     if (!Resolve(uid, ref component))
    //         return;

    //     if (TryComp<FaxableObjectComponent>(component.PaperSlot.Item, out var faxable))
    //         component.InsertingState = faxable.InsertingState;


    //     if (component.InsertingTimeRemaining > 0)
    //     {
    //         _appearanceSystem.SetData(uid, CharacterDocumentConsoleVisuals.VisualState, CharacterDocumentConsoleVisualState.Inserting);
    //         Dirty(uid, component);
    //     }
    //     else if (component.PrintingTimeRemaining > 0)
    //         _appearanceSystem.SetData(uid, CharacterDocumentConsoleVisuals.VisualState, CharacterDocumentConsoleVisualState.Printing);
    //     else
    //         _appearanceSystem.SetData(uid, CharacterDocumentConsoleVisuals.VisualState, CharacterDocumentConsoleVisualState.Normal);
    // }
}

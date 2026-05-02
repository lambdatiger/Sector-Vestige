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
using Content.Shared.IdentityManagement;
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
using Content.Server.CriminalRecords.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.StationRecords.Systems;
using Content.Server.Station.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Radio;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

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
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private static readonly ProtoId<RadioChannelPrototype> SecurityChannel = "Security";



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
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentSecurityStatus>(OnSecurityStatusChange);

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
                var filteredDocs = docComp.Documents
                    .Where(d => d.Value.DocType == (int)comp.DocumentType)
                    .ToDictionary(d => d.Key, d => d.Value);

                bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
                var state = new CharacterDocumentConsoleState(netPlayerEntities, comp.SelectedPlayer, filteredDocs, null, paperinserted, comp.DocumentType);
                _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, state);
            }
            else
            {
                bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
                var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null, paperinserted, comp.DocumentType);
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
        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null, paperinserted, comp.DocumentType);
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
        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null, paperinserted, comp.DocumentType);
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

        var filteredDocs = documentComponent.Documents
            .Where(d => d.Value.DocType == (int)comp.DocumentType)
            .ToDictionary(d => d.Key, d => d.Value);

        comp.SelectedPlayer = args.Player;
        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;

        var (secStatus, secReason) = comp.DocumentType == DocumentType.Security
            ? GetCriminalStatus(uid, documentComponent)
            : (SecurityStatus.None, null);

        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, filteredDocs, null, paperinserted, comp.DocumentType, secStatus, secReason);
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

        var filteredDocs = documentComponent.Documents
            .Where(d => d.Value.DocType == (int)comp.DocumentType)
            .ToDictionary(d => d.Key, d => d.Value);

        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;

        var (secStatus, secReason) = comp.DocumentType == DocumentType.Security
            ? GetCriminalStatus(uid, documentComponent)
            : (SecurityStatus.None, null);

        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, filteredDocs, selecteddoc, paperinserted, comp.DocumentType, secStatus, secReason);
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

        var actorName = GetActorIdentity(args.Actor);

        var providedDocument = new CharacterDocument()
        {
            DocTitle = args.DocTitle,
            DocAuthor = actorName,
            DocLastEditedBy = actorName,
            DocContent = paperComponent.Content,
            DocType = (int)comp.DocumentType,
            DocStamps = stamps
        };

        _audio.PlayPvs(comp.SuccessSound, uid);
        await _characterDocumentSystem.AddDocument(player, providedDocument);
    }

    private string GetActorIdentity(EntityUid actor)
    {
        var ev = new TryGetIdentityShortInfoEvent(null, actor);
        RaiseLocalEvent(ev);
        var title = ev.Title ?? "Unknown";
        var jobIdx = title.IndexOf(" (", StringComparison.Ordinal);
        return jobIdx > 0 ? title[..jobIdx] : title;
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
        string paper = string.Empty;
        switch (comp.DocumentType)
        {
            case DocumentType.Employment:
                paper = "SVPaperDocumentationEmployment";
                break;
            case DocumentType.Security:
                paper = "SVPaperDocumentationSecurity";
                break;
            case DocumentType.Medical:
                paper = "SVPaperDocumentationMedical";
                break;
            case DocumentType.CentralCommand:
                paper = "SVPaperDocumentationCentcomm";
                break;
        }
        var printed = Spawn(paper, uid.ToCoordinates());

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
            DocLastEditedBy = GetActorIdentity(args.Actor),
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

        var filteredDocs = documentComponent.Documents
            .Where(d => d.Value.DocType == (int)comp.DocumentType)
            .ToDictionary(d => d.Key, d => d.Value);

        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, filteredDocs, newCharacterDoc, paperinserted, comp.DocumentType);
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
            var filteredDocs = docComp.Documents
                .Where(d => d.Value.DocType == (int)comp.DocumentType)
                .ToDictionary(d => d.Key, d => d.Value);

            var state = new CharacterDocumentConsoleState(netPlayerEntities, comp.SelectedPlayer, filteredDocs, comp.SelectedDocument, isPaperInserted, comp.DocumentType);
            _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, state);
        }
        else
        {
            var state = new CharacterDocumentConsoleState(netPlayerEntities, null, null, comp.SelectedDocument, isPaperInserted, comp.DocumentType);
            _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, state);
        }
    }

    /// Security area
    public void OnSecurityStatusChange(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentSecurityStatus args)
    {
        var player = GetEntity(args.Player);
        var station = _stationSystem.GetOwningStation(uid);
        if (station == null)
            return;

        if (!TryComp<StationRecordsComponent>(station, out var stationRecords))
            return;

        if (!TryComp<CharacterDocumentComponent>(player, out var docComp))
            return;

        var listing = _stationRecords.BuildListing((station.Value, stationRecords), null);
        var recordKey = listing.FirstOrDefault(x => x.Value == docComp.ProfileName);
        if (recordKey.Value == null)
            return;

        var key = new StationRecordKey(recordKey.Key, station.Value);
        if (!_criminalRecords.TryChangeStatus(key, args.Status, args.Reason, null))
            return;

        var statusMsg = args.Status switch
        {
            SecurityStatus.Wanted => $"{docComp.ProfileName} is now wanted: {args.Reason}",
            SecurityStatus.Suspected => $"{docComp.ProfileName} is now suspected: {args.Reason}",
            SecurityStatus.Hostile => $"{docComp.ProfileName} is now hostile: {args.Reason}",
            SecurityStatus.Detained => $"{docComp.ProfileName} has been detained.",
            SecurityStatus.Paroled => $"{docComp.ProfileName} has been released on parole.",
            SecurityStatus.Discharged => $"{docComp.ProfileName} has been discharged.",
            SecurityStatus.Eliminated => $"{docComp.ProfileName} has been eliminated.",
            SecurityStatus.None => $"{docComp.ProfileName} criminal status has been cleared.",
            _ => $"{docComp.ProfileName} status changed to {args.Status}."
        };
        _radio.SendRadioMessage(uid, statusMsg, SecurityChannel, uid);

        // Refresh UI so the status label updates immediately
        if (TryComp<CharacterDocumentStationComponent>(station, out var stationComp))
        {
            var netPlayerEntities = new Dictionary<NetEntity, string>();
            foreach (var (playerUid, name) in stationComp.PlayerEntities)
                netPlayerEntities.Add(GetNetEntity(playerUid), name);

            var filteredDocs = docComp.Documents
                .Where(d => d.Value.DocType == (int)comp.DocumentType)
                .ToDictionary(d => d.Key, d => d.Value);

            var (newStatus, newReason) = GetCriminalStatus(uid, docComp);
            bool paperInserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
            var refreshState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, filteredDocs, comp.SelectedDocument, paperInserted, comp.DocumentType, newStatus, newReason);
            _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, refreshState);
        }
    }

    private void RefreshSecurityConsoleState(EntityUid uid, CharacterDocumentConsoleComponent comp)
    {

    }

    private (SecurityStatus status, string? reason) GetCriminalStatus(EntityUid consoleUid, CharacterDocumentComponent docComp)
    {
        var station = _stationSystem.GetOwningStation(consoleUid);
        if (station == null)
            return (SecurityStatus.None, null);

        if (!TryComp<StationRecordsComponent>(station, out var stationRecords))
            return (SecurityStatus.None, null);

        var listing = _stationRecords.BuildListing((station.Value, stationRecords), null);
        var recordKey = listing.FirstOrDefault(x => x.Value == docComp.ProfileName);
        if (recordKey.Value == null)
            return (SecurityStatus.None, null);

        var key = new StationRecordKey(recordKey.Key, station.Value);
        if (!_stationRecords.TryGetRecord<CriminalRecord>(key, out var record, stationRecords))
            return (SecurityStatus.None, null);

        return (record.Status, record.Reason);
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

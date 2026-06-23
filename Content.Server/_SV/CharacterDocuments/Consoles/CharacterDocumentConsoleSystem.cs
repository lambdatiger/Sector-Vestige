using Content.Server.Administration.Logs;
using Content.Shared.Database;
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
using Content.Shared.Coordinates;
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

public sealed partial class CharacterDocumentConsoleSystem : EntitySystem
{

    [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private SharedStationSystem _sharedStationSystem = default!;
    [Dependency] private CharacterDocumentSystem _characterDocumentSystem = default!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paperSystem = default!;
    [Dependency] private CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private StationRecordsSystem _stationRecords = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

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
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentRestore>(OnDocumentRestore);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentPurge>(OnDocumentPurge);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentEmptyBin>(OnEmptyBin);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentDeselect>(OnDocumentDeselected);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentPrint>(OnDocumentPrint);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentEdit>(OnDocumentEdit);
        SubscribeLocalEvent<CharacterDocumentConsoleComponent, CharacterDocumentSecurityStatus>(OnSecurityStatusChange);

    }

    public void OnDocumentEdited(CharacterDocumentEditedEvent args)
    {
        RefreshAllConsoles();
    }

    /// <summary>
    ///     Rebuilds and re-pushes every console's UI state, preserving each console's own
    ///     player/document selection. Used to live-update open consoles when something they
    ///     display changes underneath them (a document edit, or the crew roster changing).
    /// </summary>
    public void RefreshAllConsoles()
    {
        var query = EntityQueryEnumerator<CharacterDocumentConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
            RefreshConsole(uid, comp);
    }

    /// <summary>
    ///     Rebuilds and re-pushes a single console's UI state, preserving its current
    ///     player/document selection.
    /// </summary>
    public void RefreshConsole(EntityUid uid, CharacterDocumentConsoleComponent comp)
    {
        var station = _sharedStationSystem.GetOwningStation(uid);
        // Cross-map consoles (e.g. CentComm) may not have an owning station entity
        // at all if their map isn't registered as a station, so we still let them
        // through and `BuildPlayerListing` aggregates from every CharacterDocumentStation.
        if (station == null && !HasComp<CrossMapDocumentAccessComponent>(uid)) return;

        if (!TryGetStationOrCrossMap(uid, station, out var stationComponent))
            return;

        var netPlayerEntities = BuildPlayerListing(uid, stationComponent);

        var player = GetEntity(comp.SelectedPlayer);
        if (TryComp<CharacterDocumentComponent>(player, out var docComp))
        {
            var filteredDocs = BuildVisibleDocs(uid, comp, docComp);

            // Re-resolve the console's selected document against the freshly mutated
            // store: an edit swaps it for the updated copy, a delete drops it. Passing
            // a blanket null here used to wipe the reader pane on every edit.
            if (comp.SelectedDocument != null)
            {
                comp.SelectedDocument = docComp.Documents.TryGetValue(comp.SelectedDocument.DocID, out var refreshedDoc)
                    ? refreshedDoc
                    : null;
            }

            bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
            var state = new CharacterDocumentConsoleState(netPlayerEntities, comp.SelectedPlayer, filteredDocs, comp.SelectedDocument, paperinserted, comp.DocumentType, additionalDocumentTypes: comp.AdditionalDocumentTypes, selectedPlayerGeneral: docComp.CharacterDocumentGeneral);
            PushState(uid, state);
        }
        else
        {
            bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
            var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null, paperinserted, comp.DocumentType, additionalDocumentTypes: comp.AdditionalDocumentTypes);
            PushState(uid, characterDocumentConsoleState);
        }
    }

    public void OnConsoleInit(EntityUid uid, CharacterDocumentConsoleComponent comp, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, "Paper", comp.PaperSlot);
    }

    public void OnBuiOpened(EntityUid uid, CharacterDocumentConsoleComponent comp, BoundUIOpenedEvent args)
    {
        var station = _sharedStationSystem.GetOwningStation(uid);
        // Cross-map consoles (e.g. CentComm) may not have an owning station entity
        // at all if their map isn't registered as a station, so we still let them
        // through and `BuildPlayerListing` aggregates from every CharacterDocumentStation.
        if (station == null && !HasComp<CrossMapDocumentAccessComponent>(uid)) return;

        if (!TryGetStationOrCrossMap(uid, station, out var stationComponent))
            return;

        var netPlayerEntities = BuildPlayerListing(uid, stationComponent);
        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;

        var player = GetEntity(comp.SelectedPlayer);
        if (TryComp<CharacterDocumentComponent>(player, out var docComp))
        {
            var filteredDocs = BuildVisibleDocs(uid, comp, docComp);

            // Re-resolve the selected document against the current store in case it was
            // edited/removed while the console sat open with no viewers.
            if (comp.SelectedDocument != null)
            {
                comp.SelectedDocument = docComp.Documents.TryGetValue(comp.SelectedDocument.DocID, out var refreshedDoc)
                    ? refreshedDoc
                    : null;
            }

            var (secStatus, secReason) = comp.DocumentType == DocumentType.Security
                ? GetCriminalStatus(uid, docComp)
                : (SecurityStatus.None, null);

            var state = new CharacterDocumentConsoleState(netPlayerEntities, comp.SelectedPlayer, filteredDocs, comp.SelectedDocument, paperinserted, comp.DocumentType, secStatus, secReason, additionalDocumentTypes: comp.AdditionalDocumentTypes, selectedPlayerGeneral: docComp.CharacterDocumentGeneral);
            PushState(uid, state);
        }
        else
        {
            var state = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null, paperinserted, comp.DocumentType, additionalDocumentTypes: comp.AdditionalDocumentTypes);
            PushState(uid, state);
        }
    }

    public void OnBuiClosed(EntityUid uid, CharacterDocumentConsoleComponent comp, BoundUIClosedEvent args)
    {
        if (_userInterfaceSystem.GetActors(uid, args.UiKey).Any())
            return;

        comp.SelectedPlayer = default;
        comp.SelectedDocument = null;

        var station = _sharedStationSystem.GetOwningStation(uid);

        if (station == null && !HasComp<CrossMapDocumentAccessComponent>(uid))
            return;

        if (!TryGetStationOrCrossMap(uid, station, out var stationComponent))
            return;

        var netPlayerEntities = BuildPlayerListing(uid, stationComponent);

        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
        var emptyState = new CharacterDocumentConsoleState(netPlayerEntities, null, null, null, paperinserted, comp.DocumentType, additionalDocumentTypes: comp.AdditionalDocumentTypes);
        PushState(uid, emptyState);
    }

    public void OnSelectedPlayer(EntityUid uid, CharacterDocumentConsoleComponent comp, SelectCharacterDocumentPlayer args)
    {
        var station = _sharedStationSystem.GetOwningStation(uid);
        // Cross-map consoles (e.g. CentComm) may not have an owning station entity
        // at all if their map isn't registered as a station, so we still let them
        // through and `BuildPlayerListing` aggregates from every CharacterDocumentStation.
        if (station == null && !HasComp<CrossMapDocumentAccessComponent>(uid)) return;

        if (!TryGetStationOrCrossMap(uid, station, out var stationComponent))
            return;

        var player = GetEntity(args.Player);
        if (!TryComp<CharacterDocumentComponent>(player, out var documentComponent))
            return;

        var netPlayerEntities = BuildPlayerListing(uid, stationComponent);

        var filteredDocs = BuildVisibleDocs(uid, comp, documentComponent);

        comp.SelectedPlayer = args.Player;
        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;

        var (secStatus, secReason) = comp.DocumentType == DocumentType.Security
            ? GetCriminalStatus(uid, documentComponent)
            : (SecurityStatus.None, null);

        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, filteredDocs, null, paperinserted, comp.DocumentType, secStatus, secReason, additionalDocumentTypes: comp.AdditionalDocumentTypes, selectedPlayerGeneral: documentComponent.CharacterDocumentGeneral);
        PushState(uid, characterDocumentConsoleState);
    }

    public void OnSelectedDocument(EntityUid uid, CharacterDocumentConsoleComponent comp, SelectCharacterDocument args)
    {
        var station = _sharedStationSystem.GetOwningStation(uid);
        // Cross-map consoles (e.g. CentComm) may not have an owning station entity
        // at all if their map isn't registered as a station, so we still let them
        // through and `BuildPlayerListing` aggregates from every CharacterDocumentStation.
        if (station == null && !HasComp<CrossMapDocumentAccessComponent>(uid)) return;

        if (!TryGetStationOrCrossMap(uid, station, out var stationComponent))
            return;

        var player = GetEntity(args.Player);
        if (!TryComp<CharacterDocumentComponent>(player, out var documentComponent))
            return;

        documentComponent.Documents.TryGetValue(args.DocID, out var selecteddoc);
        // A binned doc may only be opened on a bin-access (Central Command) console;
        // ignore the selection otherwise so a forged client can't peek at the bin.
        if (selecteddoc is { DeletedAt: not null } && !CanAccessBin(uid))
            selecteddoc = null;
        comp.SelectedDocument = selecteddoc;

        var netPlayerEntities = BuildPlayerListing(uid, stationComponent);

        var filteredDocs = BuildVisibleDocs(uid, comp, documentComponent);

        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;

        var (secStatus, secReason) = comp.DocumentType == DocumentType.Security
            ? GetCriminalStatus(uid, documentComponent)
            : (SecurityStatus.None, null);

        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, filteredDocs, selecteddoc, paperinserted, comp.DocumentType, secStatus, secReason, additionalDocumentTypes: comp.AdditionalDocumentTypes, selectedPlayerGeneral: documentComponent.CharacterDocumentGeneral);
        PushState(uid, characterDocumentConsoleState);
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

        // Multi-type consoles (Central Command) tell us which tab is active in args.DocType.
        // Single-type consoles leave it null and we fall back to the primary type. Either way
        // we sanity-check the requested type is one this console actually handles, so a
        // malicious / stale client can't scan e.g. a Syndicate doc on the medical computer.
        var scanType = args.DocType.HasValue && ConsoleHandles(comp, args.DocType.Value)
            ? args.DocType.Value
            : (int)comp.DocumentType;

        var providedDocument = new CharacterDocument()
        {
            DocTitle = args.DocTitle,
            DocAuthor = actorName,
            DocLastEditedBy = actorName,
            DocContent = paperComponent.Content,
            DocType = scanType,
            DocStamps = stamps
        };

        _audio.PlayPvs(comp.SuccessSound, uid);
        await _characterDocumentSystem.AddDocument(player, providedDocument);
    }

    private string GetActorIdentity(EntityUid actor)
    {
        var ev = new TryGetIdentityShortInfoEvent(actor, null, false);
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

        _adminLogger.Add(LogType.CharacterDocument, LogImpact.High,
            $"{args.Actor:actor} sent character document '{args.CharacterDocument.DocTitle}' (#{args.CharacterDocument.DocID}) belonging to {player:subject} to the recycling bin via {uid:console}");

        _audio.PlayPvs(comp.SuccessSound, uid);
        await _characterDocumentSystem.DeleteDocument(player, args.CharacterDocument);
    }

    public async void OnDocumentRestore(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentRestore args)
    {
        // Only Central Command terminals may pull documents back out of the bin. The client
        // hides the control on other consoles, but re-check here against forged messages.
        if (!CanAccessBin(uid))
        {
            _audio.PlayPvs(comp.ErrorSound, uid);
            return;
        }

        var player = GetEntity(args.Player);
        if (!HasComp<CharacterDocumentComponent>(player))
        {
            _audio.PlayPvs(comp.ErrorSound, uid);
            return;
        }

        _audio.PlayPvs(comp.SuccessSound, uid);
        await _characterDocumentSystem.RestoreDocument(player, args.DocID);
    }

    public async void OnDocumentPurge(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentPurge args)
    {
        // Permanent deletion is gated to Central Command terminals, same as restore. The
        // client only surfaces the control in the bin view, but re-check here against forged
        // messages. CharacterDocumentSystem.PurgeDocument additionally refuses live docs.
        if (!CanAccessBin(uid))
        {
            _audio.PlayPvs(comp.ErrorSound, uid);
            return;
        }

        var player = GetEntity(args.Player);
        if (!TryComp<CharacterDocumentComponent>(player, out var purgeDocComp))
        {
            _audio.PlayPvs(comp.ErrorSound, uid);
            return;
        }

        var purgeTitle = purgeDocComp.Documents.TryGetValue(args.DocID, out var purgeDoc) ? purgeDoc.DocTitle : "<unknown>";
        _adminLogger.Add(LogType.CharacterDocument, LogImpact.High,
            $"{args.Actor:actor} permanently deleted binned character document '{purgeTitle}' (#{args.DocID}) belonging to {player:subject} via {uid:console}");

        _audio.PlayPvs(comp.SuccessSound, uid);
        await _characterDocumentSystem.PurgeDocument(player, args.DocID);
    }

    public async void OnEmptyBin(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentEmptyBin args)
    {
        if (!CanAccessBin(uid))
        {
            _audio.PlayPvs(comp.ErrorSound, uid);
            return;
        }

        var player = GetEntity(args.Player);
        if (!TryComp<CharacterDocumentComponent>(player, out var emptyBinDocComp))
        {
            _audio.PlayPvs(comp.ErrorSound, uid);
            return;
        }

        var binnedCount = emptyBinDocComp.Documents.Count(d => d.Value.DeletedAt != null);
        _adminLogger.Add(LogType.CharacterDocument, LogImpact.High,
            $"{args.Actor:actor} emptied the recycling bin ({binnedCount} document(s)) of {player:subject} via {uid:console}");

        _audio.PlayPvs(comp.SuccessSound, uid);
        await _characterDocumentSystem.EmptyBin(player);
    }

    public void OnDocumentPrint(EntityUid uid, CharacterDocumentConsoleComponent comp, CharacterDocumentPrint args)
    {
        // Prefer the doc the client actually clicked Print on (carried in the message).
        // Multi-type consoles (e.g. Central Command) need this so a Security doc printed
        // from the CentCom terminal still uses the Security header rather than the
        // console's primary DocumentType.
        var printDoc = args.CharacterDocument ?? comp.SelectedDocument;
        var printType = printDoc != null
            ? (DocumentType)printDoc.DocType
            : comp.DocumentType;

        var paper = printType switch
        {
            DocumentType.Employment => "SVPaperDocumentationEmployment",
            DocumentType.Security => "SVPaperDocumentationSecurity",
            DocumentType.Medical => "SVPaperDocumentationMedical",
            DocumentType.CentralCommand => "SVPaperDocumentationCentcomm",
            DocumentType.Syndicate => "SVPaperDocumentationSyndicate",
            _ => "SVPaperDocumentation",
        };
        var printed = Spawn(paper, uid.ToCoordinates());

        var stamps = new List<StampDisplayInfo>();
        string stampState = string.Empty;
        if (printDoc?.DocStamps is { Count: > 0 } docStamps)
        {
            stampState = docStamps[0].DocStampState;
            foreach (var stamp in docStamps)
                stamps.Add(stamp.DocStamp);
        }

        if (!TryComp<PaperComponent>(printed, out var paperComponent))
            return;
        else
        {
            _metaData.SetEntityName(printed, printDoc?.DocTitle ?? "Error in Printing, please report to NT R&D");
            // Balance the markup so a document saved before this fix (or via a path that skipped
            // balancing) can't print a piece of paper that crashes the paper UI when opened.
            var printContent = printDoc?.DocContent is { } docContent
                ? CharacterDocumentMarkup.Balance(docContent)
                : "Error in Printing, please report to NT R&D";
            _paperSystem.SetContent(printed, printContent);
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
        // Cross-map consoles (e.g. CentComm) may not have an owning station entity
        // at all if their map isn't registered as a station, so we still let them
        // through and `BuildPlayerListing` aggregates from every CharacterDocumentStation.
        if (station == null && !HasComp<CrossMapDocumentAccessComponent>(uid)) return;

        if (!TryGetStationOrCrossMap(uid, station, out var stationComponent))
            return;

        if (!TryComp<CharacterDocumentComponent>(player, out var documentComponent))
            return;

        var netPlayerEntities = BuildPlayerListing(uid, stationComponent);

        await _characterDocumentSystem.UpdateDocument(player, newCharacterDoc);
        comp.SelectedDocument = documentComponent.Documents.TryGetValue(newCharacterDoc.DocID, out var reloadedDoc)
            ? reloadedDoc
            : newCharacterDoc;

        var filteredDocs = BuildVisibleDocs(uid, comp, documentComponent);

        bool paperinserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
        var characterDocumentConsoleState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, filteredDocs, newCharacterDoc, paperinserted, comp.DocumentType, additionalDocumentTypes: comp.AdditionalDocumentTypes, selectedPlayerGeneral: documentComponent.CharacterDocumentGeneral);
        PushState(uid, characterDocumentConsoleState);
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
        // Cross-map consoles (e.g. CentComm) may not have an owning station entity
        // at all if their map isn't registered as a station, so we still let them
        // through and `BuildPlayerListing` aggregates from every CharacterDocumentStation.
        if (station == null && !HasComp<CrossMapDocumentAccessComponent>(uid)) return;

        if (!TryGetStationOrCrossMap(uid, station, out var stationComponent))
            return;

        var netPlayerEntities = BuildPlayerListing(uid, stationComponent);

        var isPaperInserted = comp.PaperSlot.Item.HasValue;
        if (isPaperInserted)
            _itemSlotsSystem.SetLock(uid, comp.PaperSlot, false);

        var player = GetEntity(comp.SelectedPlayer);

        if (TryComp<CharacterDocumentComponent>(player, out var docComp))
        {
            var filteredDocs = BuildVisibleDocs(uid, comp, docComp);

            var state = new CharacterDocumentConsoleState(netPlayerEntities, comp.SelectedPlayer, filteredDocs, comp.SelectedDocument, isPaperInserted, comp.DocumentType, additionalDocumentTypes: comp.AdditionalDocumentTypes, selectedPlayerGeneral: docComp.CharacterDocumentGeneral);
            PushState(uid, state);
        }
        else
        {
            var state = new CharacterDocumentConsoleState(netPlayerEntities, null, null, comp.SelectedDocument, isPaperInserted, comp.DocumentType, additionalDocumentTypes: comp.AdditionalDocumentTypes);
            PushState(uid, state);
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
            var netPlayerEntities = BuildPlayerListing(uid, stationComp);

            var filteredDocs = BuildVisibleDocs(uid, comp, docComp);

            var (newStatus, newReason) = GetCriminalStatus(uid, docComp);
            bool paperInserted = comp.PaperSlot.ContainerSlot?.ContainedEntity != null;
            var refreshState = new CharacterDocumentConsoleState(netPlayerEntities, args.Player, filteredDocs, comp.SelectedDocument, paperInserted, comp.DocumentType, newStatus, newReason, additionalDocumentTypes: comp.AdditionalDocumentTypes, selectedPlayerGeneral: docComp.CharacterDocumentGeneral);
            PushState(uid, refreshState);
        }
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

    /// <summary>
    ///     True if this console can display documents of the given type — either its primary
    ///     DocumentType or any of the AdditionalDocumentTypes (used by multi-type consoles
    ///     like Central Command).
    /// </summary>
    /// <summary>
    ///     True if this console may view and restore binned (soft-deleted) documents.
    ///     Gated to Central Command terminals, which already carry
    ///     <see cref="CrossMapDocumentAccessComponent"/>.
    /// </summary>
    private bool CanAccessBin(EntityUid consoleUid) => HasComp<CrossMapDocumentAccessComponent>(consoleUid);

    /// <summary>
    ///     Pushes UI state to a console, stamping whether it may access the bin so the
    ///     client knows to offer the recycling-bin view + restore controls.
    /// </summary>
    private void PushState(EntityUid uid, CharacterDocumentConsoleState state)
    {
        state.CanAccessBin = CanAccessBin(uid);
        _userInterfaceSystem.SetUiState(uid, CharacterDocumentConsoleUiKey.Key, state);
    }

    /// <summary>
    ///     Builds the document set this console exposes to the client: filtered to the types
    ///     the console handles and, unless it can access the bin, with binned (soft-deleted)
    ///     docs hidden. Bin-access consoles receive binned docs too; the client window only
    ///     surfaces them when its bin view is toggled on.
    /// </summary>
    private Dictionary<int, CharacterDocument> BuildVisibleDocs(EntityUid consoleUid, CharacterDocumentConsoleComponent comp, CharacterDocumentComponent docComp)
    {
        var canBin = CanAccessBin(consoleUid);
        return docComp.Documents
            .Where(d => ConsoleHandles(comp, d.Value.DocType) && (canBin || d.Value.DeletedAt == null))
            .ToDictionary(d => d.Key, d => d.Value);
    }

    private static bool ConsoleHandles(CharacterDocumentConsoleComponent comp, int docType)
    {
        if (docType == (int)comp.DocumentType)
            return true;
        for (var i = 0; i < comp.AdditionalDocumentTypes.Count; i++)
        {
            if (docType == (int)comp.AdditionalDocumentTypes[i])
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Build the (NetEntity → name) crew listing this console should expose.
    ///     Consoles with <see cref="CrossMapDocumentAccessComponent"/> aggregate
    ///     every station's roster (so CentComm terminals see main-station crew
    ///     even though they're on a different map). Everyone else gets only
    ///     their owning station's roster.
    /// </summary>
    private Dictionary<NetEntity, string> BuildPlayerListing(EntityUid consoleUid, CharacterDocumentStationComponent? ownStation)
    {
        var listing = new Dictionary<NetEntity, string>();

        if (HasComp<CrossMapDocumentAccessComponent>(consoleUid))
        {
            var query = EntityQueryEnumerator<CharacterDocumentStationComponent>();
            while (query.MoveNext(out _, out var anyStation))
            {
                foreach (var (playerUid, name) in anyStation.PlayerEntities)
                    listing[GetNetEntity(playerUid)] = name;
            }
            return listing;
        }

        if (ownStation == null)
            return listing;

        foreach (var (playerUid, name) in ownStation.PlayerEntities)
            listing[GetNetEntity(playerUid)] = name;
        return listing;
    }

    /// <summary>
    ///     Returns true if the console can proceed with serving a request.
    ///     For normal consoles that means the owning station must carry a
    ///     <see cref="CharacterDocumentStationComponent"/>. Cross-map consoles
    ///     bypass that requirement since they aggregate from every station.
    /// </summary>
    private bool TryGetStationOrCrossMap(EntityUid consoleUid, EntityUid? station, out CharacterDocumentStationComponent? stationComp)
    {
        stationComp = null;
        if (station != null)
            TryComp(station.Value, out stationComp);
        return stationComp != null || HasComp<CrossMapDocumentAccessComponent>(consoleUid);
    }
}

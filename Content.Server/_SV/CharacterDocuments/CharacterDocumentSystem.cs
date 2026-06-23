using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.StationRecords.Systems;
using Content.Server._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.StationRecords;
using Robust.Shared.Player;
using Robust.Server.Player;
using Robust.Shared.Network;
using Content.Server._SV.CharacterDocuments.Consoles;
using Content.Shared._SV.CharacterDocuments.Consoles;
using Content.Server.CriminalRecords.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared._SV.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._SV.CharacterDocuments;

public sealed partial class CharacterDocumentSystem : EntitySystem
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn, after: [typeof(StationRecordsSystem)]);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        // Empty the soft-delete bin of anything older than the retention window once per round.
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var days = _cfg.GetCVar(SVCCVars.CharacterDocumentBinRetentionDays);
        _ = PurgeExpiredBinAsync(TimeSpan.FromDays(Math.Max(0, days)));
    }

    private async Task PurgeExpiredBinAsync(TimeSpan retention)
    {
        var purged = await _db.PurgeExpiredSVCharacterDocumentsAsync(retention);
        if (purged > 0)
            Log.Info($"Purged {purged} expired character document(s) from the bin (retention {retention.TotalDays:0}d).");
    }

    /// <summary>
    ///     Safety net for in-round doc edits not making it to the client's lobby UI.
    ///     The normal path is <see cref="NotifyDocumentChangedAsync"/> →
    ///     <see cref="IServerPreferencesManager.RefreshPreferencesForUserAsync"/>, which
    ///     fires per-edit. If that push ever misses (target session was momentarily
    ///     detached at edit time, network blip between rounds, async exception swallowed
    ///     by an async-void handler), the DB still has the new docs but the client's
    ///     cached <c>Preferences</c> stays on pre-round state — so the lobby tab shows
    ///     stale docs until the player relogs.
    ///
    ///     Re-fetching on lobby entry guarantees the lobby is consistent with the DB
    ///     whenever a round ends (and on initial connect too, harmlessly redundant with
    ///     the one <see cref="ServerPreferencesManager.FinishLoad"/> already sends).
    /// </summary>
    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        _ = _prefs.RefreshPreferencesForUserAsync(ev.PlayerSession);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<StationRecordsComponent>(args.Station))
        {
            Log.Warning("Station does not have StationRecordsComponent PlayerDocs will not work");
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

        if (TryComp<CharacterDocumentComponent>(player, out var comp))
        {
            comp.ProfileName = args.Profile.Name;
            // Capture the account username while the session is guaranteed attached.
            // The save paths fall back to this if the player has since ghosted /
            // detached, instead of writing "Unknown" into SVProfile.PlayerName.
            if (_playerManager.TryGetSessionByEntity(player, out var session))
                comp.PlayerUsername = session.Name;
        }

        _ = LoadPlayerDocumentsAsync(player, args.Profile.Name);
    }

    private async Task LoadPlayerDocumentsAsync(EntityUid uid, string characterName)
    {
        _playerManager.TryGetSessionByEntity(uid, out var session);
        if (session == null) return;

        var netUserId = session.UserId;
        var playerName = session.Name;

        var prefs = await _db.GetPlayerPreferencesAsync(netUserId, CancellationToken.None);
        if (prefs == null)
        {
            Log.Warning($"Could not load preferences for player {playerName} ({characterName})");
            return;
        }

        var profile = prefs.Profiles.FirstOrDefault(p => p.CharacterName == characterName);
        if (profile == null)
        {
            Log.Debug($"Could not find profile '{characterName}' for player {playerName}");
            return;
        }

        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        docComp.ProfileId = profile.Id;
        var result = await _db.GetSVCharacterDocumentsAsync(profile.Id);
        if (result == null)
            return;

        docComp.Documents.Clear();
        foreach (var doc in result.Value.Documents)
        {
            var characterDoc = new CharacterDocument
            {
                DocID = doc.DocID,
                DocType = doc.DocType,
                DocTitle = doc.DocTitle,
                DocAuthor = doc.DocAuthor,
                DocLastEditedBy = doc.DocLastEditedBy,
                DocDateLastEdited = doc.DocDateLastEdited,
                DocContent = doc.DocContent,
                DocStamps = CharacterDocumentDeserializer.DeserializeStamps(doc.DocStamps),
                DeletedAt = doc.DeletedAt
            };
            docComp.Documents[doc.DocID] = characterDoc;
        }

        // SV: hydrate the General flavour block from the DB JSON column (piggybacked in tuple slot 1).
        if (result.Value.SerializedDocument != null)
        {
            try
            {
                docComp.CharacterDocumentGeneral =
                    System.Text.Json.JsonSerializer.Deserialize<CharacterDocumentGeneral>(result.Value.SerializedDocument)
                    ?? new CharacterDocumentGeneral();
                docComp.CharacterDocumentGeneral.EnsureValid();
            }
            catch
            {
                docComp.CharacterDocumentGeneral = new CharacterDocumentGeneral();
            }
        }
        else
        {
            docComp.CharacterDocumentGeneral = new CharacterDocumentGeneral();
        }
    }

    public async Task AddDocument(EntityUid uid, CharacterDocument characterDocument)
    {
        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        // Rebalance the user-authored markup before it is persisted so stored content (and the
        // paper printed from it) can never carry the unmatched closing tags that crash the
        // rich-text renderer.
        characterDocument.DocContent = CharacterDocumentMarkup.Balance(characterDocument.DocContent);

        _playerManager.TryGetSessionByEntity(uid, out var session);
        var playerName = ResolvePlayerName(docComp, session);

        var id = docComp.Documents.Count == 0 ? 1 : docComp.Documents.Keys.Max() + 1;
        characterDocument.DocID = id;
        docComp.Documents.Add(id, characterDocument);

        var dbDocs = docComp.Documents.Values.Select(doc => new SVModel.CharacterDocument
        {
            DocTitle = doc.DocTitle,
            DocAuthor = doc.DocAuthor,
            DocLastEditedBy = doc.DocLastEditedBy,
            DocContent = doc.DocContent,
            DocDateLastEdited = doc.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(doc.DocStamps),
            DocType = doc.DocType,
            DeletedAt = doc.DeletedAt,
            ProfileId = docComp.ProfileId
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync(docComp.ProfileId, playerName, docComp.ProfileName, dbDocs);
        await NotifyDocumentChangedAsync(session);
    }

    /// <summary>
    ///     Tells every open console to redraw AND pushes fresh preferences down to the
    ///     affected player so their lobby Documents tab + customization menu reflect
    ///     the change without requiring a reconnect.
    /// </summary>
    private async Task NotifyDocumentChangedAsync(ICommonSession? session)
    {
        RaiseLocalEvent(new CharacterDocumentEditedEvent());
        if (session != null)
            await _prefs.RefreshPreferencesForUserAsync(session);
    }

    public async Task DeleteDocument(EntityUid uid, CharacterDocument characterDocument)
    {
        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        _playerManager.TryGetSessionByEntity(uid, out var session);
        var playerName = ResolvePlayerName(docComp, session);

        // Soft delete: the doc is hidden from normal listings but kept in the store with a
        // deletion timestamp so Central Command / admins can still review or restore it.
        // A background sweep purges it permanently once it outlives the retention window.
        // We keep it in the in-memory dict (carrying DeletedAt) so the replace-all save below
        // writes it back binned rather than dropping it.
        if (docComp.Documents.TryGetValue(characterDocument.DocID, out var stored))
            stored.DeletedAt = DateTime.UtcNow;
        else
            return;

        var dbDocs = docComp.Documents.Values.Select(doc => new SVModel.CharacterDocument
        {
            DocTitle = doc.DocTitle,
            DocAuthor = doc.DocAuthor,
            DocLastEditedBy = doc.DocLastEditedBy,
            DocContent = doc.DocContent,
            DocDateLastEdited = doc.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(doc.DocStamps),
            DocType = doc.DocType,
            DeletedAt = doc.DeletedAt,
            ProfileId = docComp.ProfileId
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync(docComp.ProfileId, playerName, docComp.ProfileName, dbDocs);
        await NotifyDocumentChangedAsync(session);
    }

    /// <summary>
    ///     Restores a binned (soft-deleted) document back into normal view by clearing its
    ///     deletion timestamp. Only reachable from privileged flows (Central Command console,
    ///     admin EUI) — see the access check in the console handler.
    /// </summary>
    public async Task RestoreDocument(EntityUid uid, int docId)
    {
        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        if (!docComp.Documents.TryGetValue(docId, out var stored) || stored.DeletedAt == null)
            return;

        _playerManager.TryGetSessionByEntity(uid, out var session);
        var playerName = ResolvePlayerName(docComp, session);

        stored.DeletedAt = null;

        var dbDocs = docComp.Documents.Values.Select(doc => new SVModel.CharacterDocument
        {
            DocTitle = doc.DocTitle,
            DocAuthor = doc.DocAuthor,
            DocLastEditedBy = doc.DocLastEditedBy,
            DocContent = doc.DocContent,
            DocDateLastEdited = doc.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(doc.DocStamps),
            DocType = doc.DocType,
            DeletedAt = doc.DeletedAt,
            ProfileId = docComp.ProfileId
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync(docComp.ProfileId, playerName, docComp.ProfileName, dbDocs);
        await NotifyDocumentChangedAsync(session);
    }

    /// <summary>
    ///     Permanently deletes a single binned (soft-deleted) document, bypassing the
    ///     retention window. Only binned docs may be purged — a live document must be
    ///     sent to the bin first. Irreversible. Only reachable from privileged flows
    ///     (Central Command console, admin EUI) — see the access check in the handlers.
    /// </summary>
    public async Task PurgeDocument(EntityUid uid, int docId)
    {
        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        // Guard: only binned docs can be permanently deleted, matching the UI which only
        // offers the control from the bin view. Refuse to purge a live document.
        if (!docComp.Documents.TryGetValue(docId, out var stored) || stored.DeletedAt == null)
            return;

        _playerManager.TryGetSessionByEntity(uid, out var session);
        var playerName = ResolvePlayerName(docComp, session);

        docComp.Documents.Remove(docId);

        await PersistAllDocumentsAsync(docComp, playerName);
        await NotifyDocumentChangedAsync(session);
    }

    /// <summary>
    ///     Permanently deletes every binned (soft-deleted) document for the player — the
    ///     "empty the recycling bin" action. Live documents are left untouched. Irreversible.
    ///     Only reachable from privileged flows — see the access check in the handlers.
    /// </summary>
    public async Task EmptyBin(EntityUid uid)
    {
        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        var binnedIds = docComp.Documents
            .Where(kv => kv.Value.DeletedAt != null)
            .Select(kv => kv.Key)
            .ToList();

        if (binnedIds.Count == 0)
            return;

        _playerManager.TryGetSessionByEntity(uid, out var session);
        var playerName = ResolvePlayerName(docComp, session);

        foreach (var id in binnedIds)
            docComp.Documents.Remove(id);

        await PersistAllDocumentsAsync(docComp, playerName);
        await NotifyDocumentChangedAsync(session);
    }

    /// <summary>
    ///     Writes the player's full in-memory document set back to the DB. The DB save is a
    ///     replace-all, so removing entries from <see cref="CharacterDocumentComponent.Documents"/>
    ///     before calling this permanently drops them.
    /// </summary>
    private async Task PersistAllDocumentsAsync(CharacterDocumentComponent docComp, string playerName)
    {
        var dbDocs = docComp.Documents.Values.Select(doc => new SVModel.CharacterDocument
        {
            DocTitle = doc.DocTitle,
            DocAuthor = doc.DocAuthor,
            DocLastEditedBy = doc.DocLastEditedBy,
            DocContent = doc.DocContent,
            DocDateLastEdited = doc.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(doc.DocStamps),
            DocType = doc.DocType,
            DeletedAt = doc.DeletedAt,
            ProfileId = docComp.ProfileId
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync(docComp.ProfileId, playerName, docComp.ProfileName, dbDocs);
    }

    public async Task UpdateDocument(EntityUid uid, CharacterDocument characterDocument)
    {
        if (!TryComp<CharacterDocumentComponent>(uid, out var docComp))
            return;

        // Rebalance the user-authored markup before it is persisted (see AddDocument).
        characterDocument.DocContent = CharacterDocumentMarkup.Balance(characterDocument.DocContent);

        _playerManager.TryGetSessionByEntity(uid, out var session);
        var playerName = ResolvePlayerName(docComp, session);

        docComp.Documents[characterDocument.DocID] = characterDocument;

        var dbDocs = docComp.Documents.Values.Select(doc => new SVModel.CharacterDocument
        {
            DocTitle = doc.DocTitle,
            DocAuthor = doc.DocAuthor,
            DocLastEditedBy = doc.DocLastEditedBy,
            DocContent = doc.DocContent,
            DocDateLastEdited = doc.DocDateLastEdited,
            DocStamps = CharacterDocumentSerializer.SerializeStamp(doc.DocStamps),
            DocType = doc.DocType,
            DeletedAt = doc.DeletedAt,
            ProfileId = docComp.ProfileId
        }).ToList();

        await _db.SaveSVCharacterDocumentsAsync(docComp.ProfileId, playerName, docComp.ProfileName, dbDocs);
        await NotifyDocumentChangedAsync(session);
    }

    /// <summary>
    ///     Resolves the account username to persist as <c>SVProfile.PlayerName</c>.
    ///     Prefers the username captured on the component at spawn (stable for the
    ///     whole round, regardless of ghosting). Falls back to the live session if
    ///     that's somehow empty, and finally to "Unknown" — which should now be
    ///     genuinely unreachable for normally-spawned players.
    /// </summary>
    private static string ResolvePlayerName(CharacterDocumentComponent docComp, ICommonSession? session)
    {
        if (!string.IsNullOrEmpty(docComp.PlayerUsername))
            return docComp.PlayerUsername;
        return session?.Name ?? "Unknown";
    }
}
public sealed class CharacterDocumentEditedEvent : EntityEventArgs;

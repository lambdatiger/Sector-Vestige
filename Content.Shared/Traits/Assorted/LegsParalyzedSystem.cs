using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Shared.Traits.Assorted;

public sealed partial class LegsParalyzedSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private StandingStateSystem _standingSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LegsParalyzedComponent, BuckledEvent>(OnBuckled);
        SubscribeLocalEvent<LegsParalyzedComponent, UnbuckledEvent>(OnUnbuckled);
        SubscribeLocalEvent<LegsParalyzedComponent, ThrowPushbackAttemptEvent>(OnThrowPushbackAttempt);
        SubscribeLocalEvent<LegsParalyzedComponent, StandUpAttemptEvent>(OnStandAttempt);
    }

    // Afterlight Start
    private void OnStandAttempt(Entity<LegsParalyzedComponent> ent, ref StandUpAttemptEvent args)
    {
        args.Message = (Loc.GetString("al-paraplegia-component-cannot-stand-message"), PopupType.SmallCaution);
        args.Autostand = false;
        args.Cancelled = true;
    }
    // Afterlight End

    private void OnStartup(EntityUid uid, LegsParalyzedComponent component, ComponentStartup args)
    {
        // TODO: In future probably must be surgery related wound
        _movementSpeedModifierSystem.ChangeBaseSpeed(uid, 0, 1, 20);
    }

    private void OnShutdown(EntityUid uid, LegsParalyzedComponent component, ComponentShutdown args)
    {
        _standingSystem.Stand(uid);
    }

    private void OnBuckled(EntityUid uid, LegsParalyzedComponent component, ref BuckledEvent args)
    {
        _standingSystem.Stand(uid);
    }

    private void OnUnbuckled(EntityUid uid, LegsParalyzedComponent component, ref UnbuckledEvent args)
    {
        _standingSystem.Down(uid);
    }

    // RMC wheelchair port
    // you can atleast drag your body around on your arms, yo
//    private void OnUpdateCanMoveEvent(EntityUid uid, LegsParalyzedComponent component, UpdateCanMoveEvent args)
//    {
//        args.Cancel();
//    }

    private void OnThrowPushbackAttempt(EntityUid uid, LegsParalyzedComponent component, ThrowPushbackAttemptEvent args)
    {
        args.Cancel();
    }
}

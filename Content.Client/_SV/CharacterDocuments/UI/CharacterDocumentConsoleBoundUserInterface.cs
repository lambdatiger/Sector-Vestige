using Content.Shared._SV.CharacterDocuments;
using Content.Shared._SV.CharacterDocuments.Consoles;
using Content.Shared.Security;
using Robust.Client.UserInterface;

namespace Content.Client._SV.CharacterDocuments.UI;

public sealed class CharacterDocumentConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CharacterDocumentConsoleWindow? _window;

    public CharacterDocumentConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CharacterDocumentConsoleWindow>();
        _window.OnPlayerSelected += player => SendMessage(new SelectCharacterDocumentPlayer { Player = player });
        _window.OnDocumentSelected += (player, docId) => SendMessage(new SelectCharacterDocument { Player = player, DocID = docId });
        _window.OnButtonScanPressed += (player, title) => SendMessage(new CharacterDocumentScan { Player = player, DocTitle = title });
        _window.OnButtonPrintPressed += (player, doc) => SendMessage(new CharacterDocumentPrint { Player = player, CharacterDocument = doc });
        _window.OnButtonDeletePressed += (player, doc) => SendMessage(new CharacterDocumentDelete { Player = player, CharacterDocument = doc });
        _window.OnButtonEditPressed += (player, doc) => SendMessage(new CharacterDocumentEdit { Player = player, CharacterDocument = doc });
        _window.OnDocumentDeselected += () => SendMessage(new CharacterDocumentDeselect());
        _window.OnStatusButtonPressed += player =>
        {
            var popup = new CharacterDocumentStatusPopup();
            popup.OnConfirmed += (status, reason) =>
                SendMessage(new CharacterDocumentSecurityStatus { Player = player, Status = status, Reason = reason });
            popup.OpenCentered();
        };

    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CharacterDocumentConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }
}

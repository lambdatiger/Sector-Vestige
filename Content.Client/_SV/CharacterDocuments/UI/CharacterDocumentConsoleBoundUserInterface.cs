using Content.Shared._SV.CharacterDocuments.Consoles;
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
        _window.OnDocumentSelected += document => SendMessage(new SelectCharacterDocument { DocID = document });

    }


    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CharacterDocumentConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }
}

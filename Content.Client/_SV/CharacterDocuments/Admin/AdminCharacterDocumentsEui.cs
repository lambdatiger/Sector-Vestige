using Content.Client.Eui;
using Content.Shared._SV.CharacterDocuments.Admin;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._SV.CharacterDocuments.Admin;

[UsedImplicitly]
public sealed class AdminCharacterDocumentsEui : BaseEui
{
    private readonly AdminCharacterDocumentsWindow _window;

    public AdminCharacterDocumentsEui()
    {
        _window = new AdminCharacterDocumentsWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.OnRefresh = () => SendMessage(new AdminSVRefreshMsg());
        _window.OnEdit = (pid, doc) => SendMessage(new AdminSVDocumentEditMsg { ProfileId = pid, Document = doc });
        _window.OnDelete = (pid, did) => SendMessage(new AdminSVDocumentDeleteMsg { ProfileId = pid, DocId = did });
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AdminCharacterDocumentsEuiState s)
            return;

        _window.SetData(s.Profiles);
    }
}

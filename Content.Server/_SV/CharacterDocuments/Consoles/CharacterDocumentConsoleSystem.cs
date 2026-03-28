
using Content.Server._SV.CharacterDocuments;
using Content.Server._SV.CharacterDocuments.Consoles;
using Content.Shared.GameTicking;

public sealed class CharacterDocumentConsoleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterDocumentEditedEvent>(OnDocumentEdited);

    }

    public void OnDocumentEdited(CharacterDocumentEditedEvent args)
    {

    }

}

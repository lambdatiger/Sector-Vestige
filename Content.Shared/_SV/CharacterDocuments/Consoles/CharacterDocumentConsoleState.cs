using Content.Shared._SV.CharacterDocuments.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._SV.CharacterDocuments.Consoles;

[Serializable, NetSerializable]
public sealed class CharacterDocumentConsoleState : BoundUserInterfaceState
{
    public Dictionary<EntityUid, string> PlayerList = new();
    public EntityUid? SelectedPlayer;
    public Dictionary<int, CharacterDocument>? SelectedPlayerDocuments;
    public CharacterDocument? SelectedDocument;

    public CharacterDocumentConsoleState(Dictionary<EntityUid, string> playerlist, EntityUid? selectedplayer,
        Dictionary<int, CharacterDocument>? selectedplayerdocuments, CharacterDocument? selecteddocument)
    {
        PlayerList = playerlist;
        SelectedPlayer = selectedplayer;
        SelectedPlayerDocuments = selectedplayerdocuments;
        SelectedDocument = selecteddocument;

    }
}

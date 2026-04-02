using Content.Shared._SV.CharacterDocuments.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._SV.CharacterDocuments.Consoles;

[Serializable, NetSerializable]
public sealed class CharacterDocumentConsoleState : BoundUserInterfaceState
{
    public Dictionary<NetEntity, string> PlayerList = new();
    public NetEntity? SelectedPlayer;
    public Dictionary<int, CharacterDocument>? SelectedPlayerDocuments;
    public CharacterDocument? SelectedDocument;
    public bool PaperInserted;

    public CharacterDocumentConsoleState(Dictionary<NetEntity, string> playerlist, NetEntity? selectedplayer,
        Dictionary<int, CharacterDocument>? selectedplayerdocuments, CharacterDocument? selecteddocument, bool paperinserted)
    {
        PlayerList = playerlist;
        SelectedPlayer = selectedplayer;
        SelectedPlayerDocuments = selectedplayerdocuments;
        SelectedDocument = selecteddocument;
        PaperInserted = paperinserted;
    }
}

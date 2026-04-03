using Robust.Shared.Serialization;

namespace Content.Shared._SV.CharacterDocuments.Consoles;

[Serializable, NetSerializable]
public enum CharacterDocumentConsoleVisuals : byte
{
    VisualState,
}

[Serializable, NetSerializable]
public enum CharacterDocumentConsoleVisualState : byte
{
    Normal,
    Inserting,
    Printing
}

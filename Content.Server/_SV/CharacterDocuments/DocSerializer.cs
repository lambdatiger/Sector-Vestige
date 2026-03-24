using System.Text.Json;
using Content.Server.Database;
using Content.Shared.Paper;

namespace Content.Server._SV.CharacterDocuments;

public static class CharacterDocumentSerializer
{
    public static JsonDocument SerializeDocument(IReadOnlyCollection<SVModel.CharacterDocument> characterDocuments)
    {
        var serializeddoc = JsonSerializer.Serialize(characterDocuments);
        return JsonDocument.Parse(serializeddoc);
    }

    /// <summary>
    /// Takes a StampDisplayInfo object and turns it into a JSON
    /// </summary>
    /// <param name="stamp"></param>
    /// <returns></returns>
    public static string SerializeStamp(StampDisplayInfo stamp)
    {
        return JsonSerializer.Serialize(stamp);
    }
}

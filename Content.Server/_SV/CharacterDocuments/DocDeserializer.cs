using System.Text.Json;
using Content.Server.Database;
using Content.Shared.Paper;

namespace Content.Server._SV.CharacterDocuments;

public static class CharacterDocumentDeserializer
{
    public static List<SVModel.CharacterDocument> DeserializeDocument(JsonDocument serializedDocument)
    {
        return JsonSerializer.Deserialize<List<SVModel.CharacterDocument>>(serializedDocument)
            ?? new List<SVModel.CharacterDocument>();
    }

    /// <summary>
    /// Takes a JSON string from SerializeStamps and converts it back into the StampDisplayInfo Object
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static StampDisplayInfo DeserializeStamps(string data)
    {
        return JsonSerializer.Deserialize<StampDisplayInfo>(data);
    }
}


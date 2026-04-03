using System.Text.Json;
using Content.Server.Database;
using Content.Shared._SV.CharacterDocuments;
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
    /// Takes a JSON string from SerializeStamps and converts it back into the CharacterDocumentStamp Object
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static List<CharacterDocumentStamp> DeserializeStamps(string data)
    {
        var options = new JsonSerializerOptions { IncludeFields = true };
        return JsonSerializer.Deserialize<List<CharacterDocumentStamp>>(data, options) ?? new List<CharacterDocumentStamp>();
    }
}


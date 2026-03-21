using System.Collections.Generic;
using System.Text.Json;
using Content.Server.Database;

namespace Content.Server._SV.CharacterDocuments;

public static class CharacterDocumentDeserializer
{
    public static List<SVModel.CharacterDocument> DocumentDeserializer(JsonDocument serializedDocument)
    {
        return JsonSerializer.Deserialize<List<SVModel.CharacterDocument>>(serializedDocument)
            ?? new List<SVModel.CharacterDocument>();
    }

}


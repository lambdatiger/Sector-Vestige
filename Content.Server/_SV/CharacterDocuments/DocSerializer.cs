using System.Collections.Generic;
using System.Text.Json;
using Content.Server.Database;

namespace Content.Server._SV.CharacterDocuments;

public static class CharacterDocumentSerializer
{
    public static JsonDocument DocumentSerializer(IReadOnlyCollection<TestModel.CharacterDocument> characterDocuments)
    {
        var serializeddoc = JsonSerializer.Serialize(characterDocuments);
        return JsonDocument.Parse(serializeddoc);
    }
}

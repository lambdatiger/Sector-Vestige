using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public static class SVModel
{
    [Table("sv_profiles")]
    public class SVProfile
    {
        [Key]
        public int SVProfileID { get; set; }
        public int ProfileId { get; set; }
        [JsonIgnore]
        public Profile Profile { get; set; } = null!;

        /// <summary>
        /// The player's username. Updated on every spawn.
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// The character's name.
        /// </summary>
        public string CharacterName { get; set; } = string.Empty;

        [Column("character_doc", TypeName = "jsonb")]
        public JsonDocument? CharacterDocument { get; set; }
        [JsonIgnore]
        public List<CharacterDocument> CharacterDocuments { get; set; } = new();
    }

    [Table("sv_character_document_entries"), Index(nameof(DocID))]
    public class CharacterDocument
    {
        [Key]
        public int DocID { get; set; }
        public int DocType { get; set; } = 0;
        public DateTime DocDateLastEdited { get; set; }
        public string DocTitle { get; set; } = null!;
        public string DocAuthor { get; set; } = null!;
        public string DocContent { get; set; } = null!;
        public string DocStamps { get; set; } = null!;

        // Profile stuff:
        [JsonIgnore]
        public SVProfile SVProfile { get; set; } = null!;
        public int SVProfileID { get; set; }
    }
}

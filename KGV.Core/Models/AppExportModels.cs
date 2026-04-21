using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KGV.Core.Models
{
    [Table("app_export_definition")]
    public sealed class AppExportDefinitionRecord : BaseModel
    {
        // No numeric id in new schema usage; key is export_key
        [Column("export_key")]
        [JsonPropertyName("export_key")]
        public string? ExportKey { get; set; }

        [Column("titel")]
        [JsonPropertyName("titel")]
        public string? Titel { get; set; }

        [Column("beschreibung")]
        [JsonPropertyName("beschreibung")]
        public string? Beschreibung { get; set; }

        [Column("quelle_typ")]
        [JsonPropertyName("quelle_typ")]
        public string? QuelleTyp { get; set; }

        [Column("quelle_name")]
        [JsonPropertyName("quelle_name")]
        public string? QuelleName { get; set; }

        [Column("aktiv")]
        [JsonPropertyName("aktiv")]
        public bool Aktiv { get; set; }

        [Column("standard_sortierung")]
        [JsonPropertyName("standard_sortierung")]
        public string? StandardSortierung { get; set; }

        [Column("standard_ausgabe")]
        [JsonPropertyName("standard_ausgabe")]
        public string? StandardAusgabe { get; set; }

        [Column("erlaubt_csv")]
        [JsonPropertyName("erlaubt_csv")]
        public bool ErlaubtCsv { get; set; }

        [Column("erlaubt_pdf")]
        [JsonPropertyName("erlaubt_pdf")]
        public bool ErlaubtPdf { get; set; }

        // Display helper
        [System.Text.Json.Serialization.JsonIgnore]
        public string DisplayText => !string.IsNullOrWhiteSpace(Titel) ? Titel! : (ExportKey ?? string.Empty);
    }

    // Converter to read the raw JSON text for a property so that arrays/objects are preserved as strings
    public class RawJsonStringConverter : System.Text.Json.Serialization.JsonConverter<string?>
    {
        public override string? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
                return null;
            using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
            return doc.RootElement.GetRawText();
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, string? value, System.Text.Json.JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            try
            {
                // try to parse the raw json and write it as JSON
                using var doc = System.Text.Json.JsonDocument.Parse(value);
                doc.RootElement.WriteTo(writer);
            }
            catch
            {
                // fallback: write as string
                writer.WriteStringValue(value);
            }
        }
    }

    [Table("app_export_filter_definition")]
    public sealed class AppExportFilterDefinitionRecord : BaseModel
    {
        [Column("export_key")]
        [JsonPropertyName("export_key")]
        public string? ExportKey { get; set; }

        [Column("filter_key")]
        [JsonPropertyName("filter_key")]
        public string? FilterKey { get; set; }

        [Column("label")]
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [Column("typ")]
        [JsonPropertyName("typ")]
        public string? Typ { get; set; }

        [Column("optionen_json")]
        [JsonProperty("optionen_json")]
        public JToken? OptionenJson { get; set; }

        [Column("pflicht")]
        [JsonPropertyName("pflicht")]
        public bool Pflicht { get; set; }

        [Column("sortierung")]
        [JsonPropertyName("sortierung")]
        public int Sortierung { get; set; }
    }

    [Table("app_export_column_definition")]
    public sealed class AppExportColumnDefinitionRecord : BaseModel
    {
        [Column("export_key")]
        [JsonPropertyName("export_key")]
        public string? ExportKey { get; set; }

        [Column("column_key")]
        [JsonPropertyName("column_key")]
        public string? ColumnKey { get; set; }

        [Column("label_kurz")]
        [JsonPropertyName("label_kurz")]
        public string? LabelKurz { get; set; }

        [Column("label_lang")]
        [JsonPropertyName("label_lang")]
        public string? LabelLang { get; set; }

        [Column("sortierung")]
        [JsonPropertyName("sortierung")]
        public int Sortierung { get; set; }

        [Column("standard_sichtbar")]
        [JsonPropertyName("standard_sichtbar")]
        public bool StandardSichtbar { get; set; }

        [Column("ist_sortierspalte")]
        [JsonPropertyName("ist_sortierspalte")]
        public bool IstSortierspalte { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? Name => ColumnKey;

        [System.Text.Json.Serialization.JsonIgnore]
        public string? Label => LabelLang ?? LabelKurz;

        [System.Text.Json.Serialization.JsonIgnore]
        public bool Visible => StandardSichtbar;

        [System.Text.Json.Serialization.JsonIgnore]
        public int SortOrder => Sortierung;
    }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PosmatraciApp.Models
{
    public enum AnswerState : byte
    {
        Unanswered = 0,
        Da = 1,
        Ne = 2,
        NA = 3
    }

    public enum CheckSeverity : byte
    {
        None = 0,
        N = 1,
        S = 2,
        V = 3,
        K = 4
    }

    public class ChecklistItemDefinition
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("noteRequired")]
        public bool NoteRequired { get; set; }

        [JsonPropertyName("allowNA")]
        public bool AllowNA { get; set; }
    }

    public class ChecklistSectionDefinition
    {
        [JsonPropertyName("sectionIndex")]
        public int SectionIndex { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("timeRange")]
        public string TimeRange { get; set; } = "";

        [JsonPropertyName("items")]
        public List<ChecklistItemDefinition> Items { get; set; } = new();
    }

    public class ChecklistDefinition
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("sections")]
        public List<ChecklistSectionDefinition> Sections { get; set; } = new();
    }
}

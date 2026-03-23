using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PosmatraciApp.Models
{
    public class LocationData
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("opstine")]
        public List<Opstina> Opstine { get; set; } = new();
    }

    public class Opstina
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("birackeJedinice")]
        public List<BirackaJedinica> BiracskeJedinice { get; set; } = new();
    }

    public class BirackaJedinica
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("opstinaId")]
        public string OpstId { get; set; } = "";

        [JsonPropertyName("biracaMesta")]
        public List<BirackoMesto> BiracaMesta { get; set; } = new();
    }

    public class BirackoMesto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("shortId")]
        public int ShortId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("address")]
        public string Address { get; set; } = "";

        [JsonPropertyName("bjId")]
        public string BjId { get; set; } = "";
    }
}

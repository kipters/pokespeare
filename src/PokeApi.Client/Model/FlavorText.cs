using System.Text.Json.Serialization;

namespace PokeApi.Client.Model
{
    public record FlavorText(string Text, Language Language, Version Version)
    {
        [JsonPropertyName("flavor_text")]
        public string Text { get; init; } = Text;
        public Language Language { get; init; } = Language;
        public Version Version { get; init; } = Version;
    }
}

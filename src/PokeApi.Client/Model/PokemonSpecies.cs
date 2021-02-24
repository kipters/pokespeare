using System.Collections.ObjectModel;

namespace PokeApi.Client.Model
{
    public record PokemonSpecies
    {
        public Collection<FlavorText> FlavorTextEntries { get; init; } = new();
    }
}

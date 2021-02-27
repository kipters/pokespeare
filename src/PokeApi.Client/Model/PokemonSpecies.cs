using System.Collections.ObjectModel;

namespace PokeApi.Client.Model
{
    public record PokemonSpecies(Collection<FlavorText> FlavorTextEntries)
    {
        public Collection<FlavorText> FlavorTextEntries { get; } = FlavorTextEntries;
    }
}

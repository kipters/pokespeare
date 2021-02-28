using Pokespeare.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pokespeare.Services
{
    /// <summary>Abstracts and aggregates Pokémon data</summary>
    public interface IPokemonRepository
    {
        /// <summary>Get descriptions for a given species, filtering for a given language</summary>
        /// <param name="name">Species name</param>
        /// <param name="language">Language to get the descriptions for</param>
        Task<Monad<ICollection<string>>> GetDescriptionForSpecies(string name, string language);
    }
}

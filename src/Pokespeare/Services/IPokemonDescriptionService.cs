using Pokespeare.Common;
using System.Threading.Tasks;

namespace Pokespeare.Services
{
    /// <summary>Service that provides Pokémon descriptions</summary>
    public interface IPokemonDescriptionService
    {
        /// <summary>
        /// Returns the Shakespearean-style form of a Pokémon's description
        /// </summary>
        /// <param name="name">The Pokémon name</param>
        Task<Monad<string>> GetShakespeareanDescription(string name);
    }
}

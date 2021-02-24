using PokeApi.Client.Model;
using Refit;
using System.Threading.Tasks;

namespace PokeApi.Client
{
    [Headers("Accept: application/json")]
    public interface IPokeApi
    {
        [Get("/pokemon-species/{name}")]
        Task<ApiResponse<PokemonSpecies>> GetPokemonSpeciesAsync(string name);

        [Get("/pokemon-species/{id}")]
        Task<ApiResponse<PokemonSpecies>> GetPokemonSpeciesAsync(int id);
    }
}

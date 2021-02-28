using Microsoft.Extensions.Logging;
using PokeApi.Client;
using Pokespeare.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pokespeare.Services
{
    internal class PokemonRepository : IPokemonRepository
    {
        private readonly ILogger<PokemonRepository> _logger;
        private readonly IPokeApi _api;
        private readonly Regex _escapeCleanerRegex;

        public PokemonRepository(ILogger<PokemonRepository> logger, IPokeApi api)
        {
            _logger = logger;
            _api = api;
            _escapeCleanerRegex = new Regex(@"[\n\t\f\a]", RegexOptions.Compiled);
        }
        public async Task<Monad<ICollection<string>>> GetDescriptionForSpecies(string name, string language)
        {
            var apiResponse = await _api.GetPokemonSpeciesAsync(name);

            return apiResponse switch
            {
                { Error: not null } => new Monad<ICollection<string>>(apiResponse.Error),
                { StatusCode: HttpStatusCode.NotFound } => new Monad<ICollection<string>>(new KeyNotFoundException()),
                { Content: null } => new Monad<ICollection<string>>(new KeyNotFoundException()),

                _ => new Monad<ICollection<string>>(
                    apiResponse.Content
                        .FlavorTextEntries
                        .Where(e => e.Language.Name == language)
                        .Select(e => _escapeCleanerRegex.Replace(e.Text, " "))
                        .ToList()
                        )
            };

            throw new NotImplementedException();
        }
    }
}

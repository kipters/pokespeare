using Microsoft.Extensions.Logging;
using Pokespeare.Common;
using Pokespeare.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Pokespeare.Services
{
    internal class PokemonDescriptionService : IPokemonDescriptionService
    {
        private readonly ILogger<PokemonDescriptionService> _logger;
        private readonly IPokemonRepository _pokemonRepo;
        private readonly ITranslationService _translationService;

        public PokemonDescriptionService(ILogger<PokemonDescriptionService> logger
            , IPokemonRepository pokemonRepo
            , ITranslationService translationService
        )
        {
            _logger = logger;
            _pokemonRepo = pokemonRepo;
            _translationService = translationService;
        }

        public async Task<Monad<string>> GetShakespeareanDescription(string name)
        {
            var descriptions = await _pokemonRepo.GetDescriptionForSpecies(name, "en");

            if (descriptions.Exception is not null)
            {
                return new Monad<string>(descriptions.Exception);
            }

            var text = descriptions.Result!.First();
            var translation = await _translationService.GetShakespeareanTranslation(text);

            return translation switch
            {
                { Result: not null } => new Monad<string>(translation.Result),
                { Exception: LimitExceededException le } => new Monad<string>(le),
                { Exception: not null } => new Monad<string>(translation.Exception),
                _ => throw new InvalidOperationException()
            };
        }
    }
}

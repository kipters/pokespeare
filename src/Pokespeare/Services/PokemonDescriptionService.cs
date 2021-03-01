using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Pokespeare.Common;
using Pokespeare.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pokespeare.Services
{
#pragma warning disable CA5394 // System.Random is more than enough here
    internal class PokemonDescriptionService : IPokemonDescriptionService
    {
        private readonly ILogger<PokemonDescriptionService> _logger;
        private readonly IPokemonRepository _pokemonRepo;
        private readonly ITranslationService _translationService;
        private readonly IFeatureManager _featureManager;
        private readonly Random _random;

        public PokemonDescriptionService(ILogger<PokemonDescriptionService> logger
            , IPokemonRepository pokemonRepo
            , ITranslationService translationService
            , IFeatureManager featureManager
        )
        {
            _logger = logger;
            _pokemonRepo = pokemonRepo;
            _translationService = translationService;
            _featureManager = featureManager;
            _random = new Random();
        }

        public async Task<Monad<string>> GetShakespeareanDescription(string name)
        {
            var descriptions = await _pokemonRepo.GetDescriptionForSpecies(name, "en");

            if (descriptions.Exception is not null)
            {
                return new Monad<string>(descriptions.Exception);
            }

            var pickRandom = await _featureManager.IsEnabledAsync(FeatureFlags.RandomDescription);
            var values = descriptions.Result!;
            _logger.LogInformation("Picking {selectionMode} description for {species}", pickRandom ? "random" : "first",
                name);
            var text = pickRandom switch
            {
                false => values.First(),
                true => values.ElementAt(_random.Next(0, values.Count))
            };
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

using Microsoft.Extensions.Logging;
using Moq;
using PokeApi.Client.Common;
using Pokespeare.Services;
using System.Collections.Generic;
using Xunit;

namespace Pokespeare.Tests.Services
{
    public class PokemonDescriptionServiceTests
    {
        private Mock<ILogger<PokemonDescriptionService>> Logger { get; }
        private Mock<IPokemonRepository> Repository { get; }
        private Mock<ITranslationService> TranslationService { get; }
        private PokemonDescriptionService Service { get; }

        public PokemonDescriptionServiceTests()
        {
            Logger = new Mock<ILogger<PokemonDescriptionService>>();
            Repository = new Mock<IPokemonRepository>();
            TranslationService = new Mock<ITranslationService>();
            Service = new PokemonDescriptionService(Logger.Object, Repository.Object, TranslationService.Object);
        }

        [Fact]
        public void ReturnsKeyNotFoundExceptionIfPokemonDoesNotExist()
        {
            Repository
                .Setup(x => x.GetDescriptionForSpecies(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Monad<ICollection<string>>(new KeyNotFoundException()))
        }
    }
}

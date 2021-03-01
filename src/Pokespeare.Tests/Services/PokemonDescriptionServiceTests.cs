using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;
using Pokespeare.Common;
using Pokespeare.Exceptions;
using Pokespeare.Services;
using Refit;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Pokespeare.Tests.Services
{
    public class PokemonDescriptionServiceTests
    {
        private Mock<ILogger<PokemonDescriptionService>> Logger { get; }
        private Mock<IPokemonRepository> Repository { get; }
        private Mock<ITranslationService> TranslationService { get; }
        private Mock<IFeatureManager> FeatureManager { get; }
        private PokemonDescriptionService Service { get; }

        public PokemonDescriptionServiceTests()
        {
            Logger = new Mock<ILogger<PokemonDescriptionService>>();
            Repository = new Mock<IPokemonRepository>();
            TranslationService = new Mock<ITranslationService>();
            FeatureManager = new Mock<IFeatureManager>();
            Service = new PokemonDescriptionService(Logger.Object, Repository.Object,
                TranslationService.Object, FeatureManager.Object);

            FeatureManager
                .Setup(f => f.IsEnabledAsync(FeatureFlags.RandomDescription))
                .ReturnsAsync(false);
        }

        [Fact]
        public async Task ReturnsKeyNotFoundExceptionIfPokemonDoesNotExistAsync()
        {
            Repository
                .Setup(x => x.GetDescriptionForSpecies(It.IsAny<string>(), "en"))
                .ReturnsAsync(new Monad<ICollection<string>>(new KeyNotFoundException()));

            var result = await Service.GetShakespeareanDescription("dummy");

            Assert.NotNull(result.Exception);
            Assert.IsType<KeyNotFoundException>(result.Exception);
        }

        [Fact]
        public async Task ReturnsFailedResultIfCantRetrieveDescriptionAsync()
        {
            Repository
                .Setup(x => x.GetDescriptionForSpecies(It.IsAny<string>(), "en"))
                .ReturnsAsync(new Monad<ICollection<string>>(new HttpRequestException()));

            var result = await Service.GetShakespeareanDescription("dummy");

            Assert.NotNull(result.Exception);
            Assert.IsType<HttpRequestException>(result.Exception);
        }

        [Fact]
        public async Task ReturnsLimitExceededIfTranslationServiceRateLimitExceeded()
        {
            Repository
                .Setup(x => x.GetDescriptionForSpecies(It.IsAny<string>(), "en"))
                .ReturnsAsync(new Monad<ICollection<string>>(new List<string> { "Dummy1", "Dummy2" }));

            TranslationService
                .Setup(x => x.GetShakespeareanTranslation(It.IsAny<string>()))
                .ReturnsAsync(new Monad<string>(new LimitExceededException()));

            var result = await Service.GetShakespeareanDescription("dummy");

            Assert.NotNull(result.Exception);
            Assert.IsType<LimitExceededException>(result.Exception);
        }

        [Fact]
        public async Task TranslatesTheFirstDescriptionAsync()
        {
            var descriptions = new List<string> { "Dummy1", "Dummy2" };
            Repository
                .Setup(x => x.GetDescriptionForSpecies(It.IsAny<string>(), "en"))
                .ReturnsAsync(new Monad<ICollection<string>>(descriptions));

            TranslationService
                .Setup(x => x.GetShakespeareanTranslation(descriptions.First()))
                .ReturnsAsync(new Monad<string>("translated dummy"))
                .Verifiable();

            var result = await Service.GetShakespeareanDescription("dummy");

            TranslationService.VerifyAll();
        }

        [Fact]
        public async Task ReturnsSuccessfulResultIfTranslationSucceedsAsync()
        {
            var descriptions = new List<string> { "Dummy1", "Dummy2" };
            Repository
                .Setup(x => x.GetDescriptionForSpecies(It.IsAny<string>(), "en"))
                .ReturnsAsync(new Monad<ICollection<string>>(descriptions));

            TranslationService
                .Setup(x => x.GetShakespeareanTranslation(descriptions.First()))
                .ReturnsAsync(new Monad<string>("translated dummy"));

            var result = await Service.GetShakespeareanDescription("dummy");

            Assert.NotNull(result.Result);
            Assert.Equal("translated dummy", result.Result);
        }

        [Fact]
        public async Task AlwaysPicksTheFirstOneWhenRandomDescriptionIsDisabled()
        {
            var descriptions = new List<string> { "Dummy1", "Dummy2" };
            Repository
                .Setup(x => x.GetDescriptionForSpecies(It.IsAny<string>(), "en"))
                .ReturnsAsync(new Monad<ICollection<string>>(descriptions));

            TranslationService
                .Setup(x => x.GetShakespeareanTranslation(descriptions.First()))
                .ReturnsAsync(new Monad<string>("translated dummy"))
                .Verifiable();

            FeatureManager
                .Setup(x => x.IsEnabledAsync(FeatureFlags.RandomDescription))
                .ReturnsAsync(false);

            var result = await Service.GetShakespeareanDescription("dummy");

            TranslationService.VerifyAll();
        }

        [Fact]
        public async Task PicksRandomDescriptionWhenRandomDescriptionsIsEnabled()
        {
            var descriptions = Enumerable.Range(1, 50)
                .Select(i => $"Dummy {i}")
                .ToList();
            Repository
                .Setup(x => x.GetDescriptionForSpecies(It.IsAny<string>(), "en"))
                .ReturnsAsync(new Monad<ICollection<string>>(descriptions));

            TranslationService
                .Setup(x => x.GetShakespeareanTranslation(It.IsAny<string>()))
                .ReturnsAsync(new Monad<string>("translated dummy"));

            FeatureManager
                .Setup(x => x.IsEnabledAsync(FeatureFlags.RandomDescription))
                .ReturnsAsync(true);

            for (var i = 0; i < 10; i++)
            {
                await Service.GetShakespeareanDescription("dummy");
            }

            var notFirstDescriptionInvocations = TranslationService
                .Invocations
                .Select(x => x.Arguments.First() as string)
                .Where(x => x != descriptions[0]);

            Assert.NotEmpty(notFirstDescriptionInvocations);
        }
    }
}

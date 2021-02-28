using Microsoft.Extensions.Logging;
using Moq;
using PokeApi.Client;
using PokeApi.Client.Model;
using Pokespeare.Services;
using Refit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Version = PokeApi.Client.Model.Version;

namespace Pokespeare.Tests.Services
{
    public class PokemonRepositoryTests
    {
        internal Mock<ILogger<PokemonRepository>> Logger { get; }
        internal Mock<IPokeApi> Api { get; }
        internal PokemonRepository Repository { get; }

        public PokemonRepositoryTests()
        {
            Logger = new Mock<ILogger<PokemonRepository>>();
            Api = new Mock<IPokeApi>();
            Repository = new PokemonRepository(Logger.Object, Api.Object);
        }

        [Fact]
        public async Task ReturnsKeyNotFoundExceptionIfResultIsNotFoundAsync()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            using var apiResponse = new ApiResponse<PokemonSpecies>(response, null, new RefitSettings(), default);
            Api
                .Setup(x => x.GetPokemonSpeciesAsync(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            var descriptions = await Repository.GetDescriptionForSpecies("dummy", "en");

            Assert.Null(descriptions.Result);
            Assert.NotNull(descriptions.Exception);
            Assert.IsType<KeyNotFoundException>(descriptions.Exception);
        }

        [Fact]
        public async Task ReturnsOnlyRequestedLanguagesAsync()
        {
            var textEntries = new Collection<FlavorText>
            {
                new FlavorText("English1", new Language("en"), new Version("red")),
                new FlavorText("English2", new Language("en"), new Version("gold")),
                new FlavorText("Italian1", new Language("it"), new Version("red")),
                new FlavorText("French1", new Language("fr"), new Version("ruby"))
            };
            var species = new PokemonSpecies(textEntries);
            using var response = new HttpResponseMessage(HttpStatusCode.OK);
            using var apiResponse = new ApiResponse<PokemonSpecies>(response, species, new RefitSettings());

            Api
                .Setup(x => x.GetPokemonSpeciesAsync(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            var result = await Repository.GetDescriptionForSpecies("dummy", "en");

            Assert.Null(result.Exception);
            Assert.NotNull(result.Result);
            Assert.All(result.Result, t =>
            {
                var entry = textEntries.Single(e => e.Text == t);
                Assert.Equal("en", entry.Language.Name);
            });
        }

        [Theory]
        [InlineData("With\nNew line", "With New line")]
        [InlineData("With\fForm feed", "With Form feed")]
        [InlineData("With\tTabulation", "With Tabulation")]
        [InlineData("With\aA beep. Yes really.", "With A beep. Yes really.")]

        public async Task ReturnedDescriptionsDontContainEscapeSequences(string escaped, string clean)
        {
            var textEntries = new Collection<FlavorText>
            {
                new FlavorText(escaped, new Language("en"), new Version("red")),
            };
            var species = new PokemonSpecies(textEntries);
            using var response = new HttpResponseMessage(HttpStatusCode.OK);
            using var apiResponse = new ApiResponse<PokemonSpecies>(response, species, new RefitSettings());

            Api
                .Setup(x => x.GetPokemonSpeciesAsync(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            var result = await Repository.GetDescriptionForSpecies("dummy", "en");

            Assert.Null(result.Exception);
            Assert.NotNull(result.Result);
            Assert.All(result.Result, t => Assert.Equal(clean, t));
        }

        [Fact]
        public async Task ReturnsFailureIfApiFailsAsync()
        {
            using var request = new HttpRequestMessage();
            using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var exception = new TestApiException(request, HttpMethod.Get, null, HttpStatusCode.InternalServerError,
                "Test reason", response.Headers, new RefitSettings(), default);
            using var apiResponse = new ApiResponse<PokemonSpecies>(response, null, new RefitSettings(), exception);
            Api
                .Setup(x => x.GetPokemonSpeciesAsync(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            var result = await Repository.GetDescriptionForSpecies("dummy", "en");

            Assert.Null(result.Result);
            Assert.NotNull(result.Exception);
            Assert.IsAssignableFrom<ApiException>(result.Exception);
        }

#pragma warning disable CA1032 // We don't need other constructors here
        internal class TestApiException : ApiException
        {
            public TestApiException(HttpRequestMessage message, HttpMethod httpMethod
                , string? content, HttpStatusCode statusCode, string? reasonPhrase
                , HttpResponseHeaders headers, RefitSettings refitSettings, Exception? innerException = null)
                : base(message, httpMethod, content, statusCode, reasonPhrase, headers, refitSettings
                    , innerException)
            {
            }
        }
#pragma warning restore CA1032
    }
}

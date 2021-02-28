using FunTranslationsApi.Client;
using FunTranslationsApi.Client.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Pokespeare.Exceptions;
using Pokespeare.Services;
using Refit;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Pokespeare.Tests.Services
{
    public class TranslationServiceTests
    {
        internal Mock<ILogger<TranslationService>> Logger { get; }
        internal Mock<IFunTranslationsApi> Api { get; }
        internal TranslationService Service { get; }

        public TranslationServiceTests()
        {
            Logger = new Mock<ILogger<TranslationService>>();
            Api = new Mock<IFunTranslationsApi>();
            Service = new TranslationService(Logger.Object, Api.Object);
        }

        [Fact]
        public async Task ReturnLimitExceededExceptionIfTooManyRequestAsync()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            using var apiResponse = new ApiResponse<TranslationResult>(response, null, new RefitSettings());

            Api
                .Setup(x => x.GetShakespeareanTranslation(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            var result = await Service.GetShakespeareanTranslation("dummy");

            Assert.Null(result.Result);
            Assert.NotNull(result.Exception);
            Assert.IsType<LimitExceededException>(result.Exception);
        }

        [Fact]
        public async Task ReturnsErrorIfTranslationFailsAsync()
        {
            using var request = new HttpRequestMessage();
            using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var exception = ApiException.Create(request, HttpMethod.Get, response, new RefitSettings()).Result;
            Api
                .Setup(x => x.GetShakespeareanTranslation(It.IsAny<string>()))
                .ThrowsAsync(exception);

            var result = await Service.GetShakespeareanTranslation("dummy");

            Assert.Null(result.Result);
            Assert.NotNull(result.Exception);
            Assert.Same(exception, result.Exception);
        }

        [Fact]
        public async Task ReturnsTranslatedTextAsync()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.OK);
            var translatedText = "TranslatedText";
            var translationResult = new TranslationResult(
                new Successes(1),
                new TranslationContent(translatedText, "dummy", "shakespeare")
            );

            using var apiResponse = new ApiResponse<TranslationResult>(response, translationResult,
                new RefitSettings(), null);

            Api
                .Setup(x => x.GetShakespeareanTranslation(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            var result = await Service.GetShakespeareanTranslation("dummy");

            Assert.Null(result.Exception);
            Assert.NotNull(result.Result);
            Assert.Equal(translatedText, result.Result);
        }
    }
}

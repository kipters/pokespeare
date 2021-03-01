using FunTranslationsApi.Client;
using FunTranslationsApi.Client.Model;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Pokespeare.Exceptions;
using Pokespeare.Services;
using Refit;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Pokespeare.Tests.Services
{
    public class TranslationServiceTests
    {
        internal Mock<ILogger<TranslationService>> Logger { get; }
        internal Mock<IFunTranslationsApi> Api { get; }
        internal Mock<IDistributedCache> Cache { get; }
        internal TranslationService Service { get; }

        public TranslationServiceTests()
        {
            Logger = new Mock<ILogger<TranslationService>>();
            Api = new Mock<IFunTranslationsApi>();
            Cache = new Mock<IDistributedCache>();
            Cache
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(byte[]));
            Service = new TranslationService(Logger.Object, Api.Object, Cache.Object);
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

        [Fact]
        public async Task DontCallApiOnCacheHitsAsync()
        {
            var fromCache = "FromCache";
            var fromCacheBytes = Encoding.UTF8.GetBytes(fromCache);

            Cache
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fromCacheBytes);

            var result = await Service.GetShakespeareanTranslation("dummy");

            Api.Verify(x => x.GetShakespeareanTranslation(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ReturnDataFromCacheOnHit()
        {
            var fromCache = "FromCache";
            var fromCacheBytes = Encoding.UTF8.GetBytes(fromCache);

            Cache
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fromCacheBytes);

            var result = await Service.GetShakespeareanTranslation("dummy");

            Assert.Null(result.Exception);
            Assert.NotNull(result.Result);
            Assert.Equal(fromCache, result.Result);
        }

        [Fact]
        public async Task CallApiOnCacheMissAsync()
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
                .Setup(x => x.GetShakespeareanTranslation("dummy"))
                .ReturnsAsync(apiResponse);

            Cache
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(byte[]));

            var result = await Service.GetShakespeareanTranslation("dummy");

            Api.Verify(x => x.GetShakespeareanTranslation("dummy"), Times.Once);
        }

        [Fact]
        public async Task ItStoresResultsInCacheAsync()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.OK);
            var translatedText = "TranslatedText";
            var translatedTextBytes = Encoding.UTF8.GetBytes(translatedText);
            var translationResult = new TranslationResult(
                new Successes(1),
                new TranslationContent(translatedText, "dummy", "shakespeare")
            );

            using var apiResponse = new ApiResponse<TranslationResult>(response, translationResult,
                new RefitSettings(), null);

            Api
                .Setup(x => x.GetShakespeareanTranslation(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            Cache
                .Setup(x => x.SetAsync(It.IsAny<string>(), translatedTextBytes,
                    It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(1)),
                    It.IsAny<CancellationToken>()))
                .Verifiable();


            var result = await Service.GetShakespeareanTranslation("dummy");

            Cache.VerifyAll();
        }
    }
}

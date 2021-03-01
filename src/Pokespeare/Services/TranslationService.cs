using FunTranslationsApi.Client;
using FunTranslationsApi.Client.Model;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Pokespeare.Common;
using Pokespeare.Exceptions;
using Refit;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pokespeare.Services
{
    internal class TranslationService : ITranslationService
    {
        private readonly ILogger<TranslationService> _logger;
        private readonly IFunTranslationsApi _translationApi;
        private readonly IDistributedCache _cache;
        private readonly SHA256 _hasher;

        public TranslationService(ILogger<TranslationService> logger
            , IFunTranslationsApi translationsApi
            , IDistributedCache cache
        )
        {
            _logger = logger;
            _translationApi = translationsApi;
            _cache = cache;
            _hasher = SHA256.Create();
        }
        public async Task<Monad<string>> GetShakespeareanTranslation(string source)
        {
            var hash = _hasher.ComputeBase64Hash(source);
            var cacheKey = $"shakespeare:{hash}";

            var cachedResult = await _cache.GetStringAsync(cacheKey);

            if (cachedResult is not null)
            {
                _logger.LogInformation("Cache hit for translation");
                return new Monad<string>(cachedResult);
            }

            ApiResponse<TranslationResult> response;
            try
            {
                response = await _translationApi.GetShakespeareanTranslation(source);
            }
            catch (ApiException e)
            {
                return new Monad<string>(e);
            }

            return response switch
            {
                { StatusCode: HttpStatusCode.TooManyRequests } => new Monad<string>(new LimitExceededException()),
                { Error: not null } => new Monad<string>(response.Error),
                { Content: null } => throw new NotImplementedException(),
                _ => await CacheAndReturnTranslatedContentAsync(response.Content.Contents.Translated)
            };

            async Task<Monad<string>> CacheAndReturnTranslatedContentAsync(string translatedString)
            {
                var lifetime = TimeSpan.FromHours(1);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = lifetime
                };
                await _cache.SetStringAsync(cacheKey, translatedString, options);

                return new Monad<string>(translatedString);
            }
        }
    }
}

using FunTranslationsApi.Client;
using FunTranslationsApi.Client.Model;
using Microsoft.Extensions.Logging;
using Pokespeare.Common;
using Pokespeare.Exceptions;
using Refit;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Pokespeare.Services
{
    internal class TranslationService : ITranslationService
    {
        private readonly ILogger<TranslationService> _logger;
        private readonly IFunTranslationsApi _translationApi;

        public TranslationService(ILogger<TranslationService> logger, IFunTranslationsApi translationsApi)
        {
            _logger = logger;
            _translationApi = translationsApi;
        }
        public async Task<Monad<string>> GetShakespeareanTranslation(string source)
        {
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
                _ => new Monad<string>(response.Content.Contents.Translated)
            };
        }
    }
}

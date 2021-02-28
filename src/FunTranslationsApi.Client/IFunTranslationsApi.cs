using FunTranslationsApi.Client.Model;
using Refit;
using System.Threading.Tasks;

namespace FunTranslationsApi.Client
{
    public interface IFunTranslationsApi
    {
        [Get("/translate/shakespeare.json")]
        Task<ApiResponse<TranslationResult>> GetShakespeareanTranslation([Query] string text);
    }
}

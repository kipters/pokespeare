using Pokespeare.Common;
using System.Threading.Tasks;

namespace Pokespeare.Services
{
    /// <summary>Wraps and abstracts translation services</summary>
    public interface ITranslationService
    {
        /// <summary>Translate a given text into Shakespearean style</summary>
        /// <param name="source">Text to translate</param>
        Task<Monad<string>> GetShakespeareanTranslation(string source);
    }
}

using System;

namespace Pokespeare.ConfigModel
{
    /// <summary>
    /// Model for FunTranslations related config
    /// </summary>
    public class FunTranslationsConfig
    {
        /// <summary>
        /// BaseUrl for the api, defaults to https://api.funtranslations.com
        /// </summary>
        public Uri BaseUrl { get; set; } = new Uri("https://api.funtranslations.com");
    }
}

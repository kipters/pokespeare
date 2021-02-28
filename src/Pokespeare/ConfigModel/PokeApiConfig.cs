using System;

namespace Pokespeare.ConfigModel
{
    /// <summary>
    /// Model for PokeApi related config
    /// </summary>
    public class PokeApiConfig
    {
        /// <summary>
        /// BaseUrl for the api, defaults to https://pokeapi.co/api/v2
        /// </summary>
        public Uri BaseUrl { get; set; } = new Uri("https://pokeapi.co/api/v2/");
    }
}

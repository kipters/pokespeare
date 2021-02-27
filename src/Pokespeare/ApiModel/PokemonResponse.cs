namespace Pokespeare.ApiModel
{
    /// <summary>A Pokémon and its description</summary>
    public record PokemonResponse(string Name, string Description)
    {
        /// <summary>The Pokémon name</summary>
        public string Name { get; } = Name;

        /// <summary>The Pokémon description</summary>
        public string Description { get; } = Description;
    }
}

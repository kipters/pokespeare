using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pokespeare.ApiModel;
using Pokespeare.Exceptions;
using Pokespeare.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pokespeare.Controllers
{
    /// <summary>
    /// Pokémon APIs
    /// </summary>
    [ApiController, Route("[controller]")]
    [Produces("application/json", "application/xml")]
    public class PokemonController : ControllerBase
    {
        private readonly ILogger<PokemonController> _logger;
        private readonly IPokemonDescriptionService _descriptionService;

        /// <param name="logger">Log provider</param>
        /// <param name="descriptionService">Pokémon description provider</param>
        public PokemonController(ILogger<PokemonController> logger
            , IPokemonDescriptionService descriptionService
        )
        {
            _logger = logger;
            _descriptionService = descriptionService;
        }

        /// <summary>
        /// Returns the Shakespearean-style description of a given Pokémon
        /// </summary>
        /// <param name="name">The Pokémon name ('mew', 'pikachu', ecc.)</param>
        /// <returns>The requested Pokémon's description</returns>
        /// <response code="200">The requested description exists</response>
        /// <response code="400">Not a valid Pokémon</response>
        /// <response code="429">Too many requests</response>
        [HttpGet("{name}")]
        [ProducesResponseType(typeof(PokemonResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetShakespeareanDescription(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

#pragma warning disable CA1308 // We actually want ToLowerInvariant
            var lowerCaseName = name.Trim().ToLowerInvariant();
#pragma warning restore CA1308

            var result = await _descriptionService.GetShakespeareanDescription(lowerCaseName);
            _logger.LogInformation("Requesting description for {name}", lowerCaseName);
            return result switch
            {
                { Result: not null } => Ok(new PokemonResponse(lowerCaseName, result.Result)),
                { Exception: KeyNotFoundException } =>
                    ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                    {
                        [nameof(name)] = new[] { "Invalid Pokémon species" }
                    })),
                { Exception: LimitExceededException } => StatusCode(StatusCodes.Status429TooManyRequests),
                { Exception: Exception e } => throw new InvalidOperationException("Invalid result", e),
                _ => throw new InvalidOperationException()
            };
        }
    }
}

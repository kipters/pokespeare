using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Pokespeare.ApiModel;
using Pokespeare.Common;
using Pokespeare.Controllers;
using Pokespeare.Exceptions;
using Pokespeare.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Pokespeare.Tests.Controllers
{
    public class PokemonControllerTests
    {
        private Mock<ILogger<PokemonController>> Logger { get; }
        private Mock<IPokemonDescriptionService> DescriptionService { get; }
        private PokemonController Controller { get; }

        public PokemonControllerTests()
        {
            Logger = new Mock<ILogger<PokemonController>>();
            DescriptionService = new Mock<IPokemonDescriptionService>();
            Controller = new PokemonController(Logger.Object, DescriptionService.Object);
        }

        [Theory]
        [InlineData("mew", "Dummy description")]
        public async Task ReturnsDescriptionOfValidPokemon(string name, string description)
        {
            DescriptionService
                .Setup(s => s.GetShakespeareanDescription(It.IsAny<string>()))
                .ReturnsAsync(new Monad<string>(description));

            var result = await Controller.GetShakespeareanDescription(name);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PokemonResponse>(okResult.Value);

            Assert.Equal(name, response.Name);
            Assert.Equal(description, response.Description);
        }

        [Theory]
        [InlineData("mew", "Dummy description")]
        public async Task ReturnedNameMatchesParameter(string name, string description)
        {
            DescriptionService
                .Setup(s => s.GetShakespeareanDescription(It.IsAny<string>()))
                .ReturnsAsync(new Monad<string>(description));

            var result = await Controller.GetShakespeareanDescription(name);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PokemonResponse>(okResult.Value);

            Assert.Equal(name, response.Name);
        }

        [Theory]
        [InlineData("mewthree")]
        public async Task ReturnsBadRequestIfPokemonDoesNotExist(string name)
        {
            DescriptionService
                .Setup(s => s.GetShakespeareanDescription(It.IsAny<string>()))
                .ReturnsAsync(new Monad<string>(new KeyNotFoundException()));

            var result = await Controller.GetShakespeareanDescription(name);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [InlineData("mewthree")]
        public async Task ReturnsTooManyRequestsIfAnyLimitIsExceeded(string name)
        {
            DescriptionService
                .Setup(s => s.GetShakespeareanDescription(It.IsAny<string>()))
                .ReturnsAsync(new Monad<string>(new LimitExceededException()));

            var result = await Controller.GetShakespeareanDescription(name);

            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status429TooManyRequests, statusCodeResult.StatusCode);
        }

        [Theory]
        [InlineData("mEw")]
        [InlineData("mew")]
        [InlineData("Mew")]
        [InlineData("MEW")]
        public async Task PokemonNameIsAlwaysPassedLowercase(string name)
        {
            DescriptionService
#pragma warning disable CA1308 // We actually want ToLowerInvariant
                .Setup(s => s.GetShakespeareanDescription(name.ToLowerInvariant()))
#pragma warning restore CA1308
                .ReturnsAsync(new Monad<string>("Dummy"))
                .Verifiable();

            var result = await Controller.GetShakespeareanDescription(name);

            DescriptionService.VerifyAll();
        }

        [Fact]
        public async Task ThrowsOnUnexpectedError()
        {
            var expected = new ArgumentException();
            DescriptionService
                .Setup(s => s.GetShakespeareanDescription(It.IsAny<string>()))
                .ReturnsAsync(new Monad<string>(expected));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await Controller.GetShakespeareanDescription("mew"));

            Assert.Same(expected, exception.InnerException);
        }

        [Fact]
        public async Task ThrowsOnIllegalResult()
        {
            DescriptionService
                .Setup(s => s.GetShakespeareanDescription(It.IsAny<string>()))
                .ReturnsAsync(new Monad<string>(result: null!));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await Controller.GetShakespeareanDescription("mew"));

            Assert.Null(exception.InnerException);
        }
    }
}

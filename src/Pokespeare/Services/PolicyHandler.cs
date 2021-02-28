using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Pokespeare.Services
{
    /// <summary>
    /// HttpClient handler that enforces a series of wrapped Polly policies:
    /// 1. Make up to six retries for calls that fail with errors 500, 502, 503 or 504.
    /// the first retry happens immediately, the following five use a decorrelated jittered backoff delay
    /// 2. Trips a circuit breaker for 30 seconds if the service responds with 500, 502, 503 and 504.
    /// </summary>
    public class PolicyHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        /// <param name="logger">ILogger implementation</param>
        /// <param name="handler">HttpMessageHandler to send actual requests to</param>
        public PolicyHandler(ILogger<PolicyHandler> logger, HttpMessageHandler handler)
            : base(handler)
        {
            // Errors that are likely to be transient
            var statusCodesWorthRetrying = new[]
            {
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout
            };

            var delay = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 6, fastFirst: true);
            var retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => statusCodesWorthRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(delay, (result, timestamp, retryCount, context) =>
                {
                    var reason = result switch
                    {
                        { Exception: not null } => result.Exception.Message,
                        { Result: not null } => $"Status code: {result.Result.StatusCode}",
                        null => "Unknown (null result)",
                        _ => "Unknown"
                    };

                    var uri = context["uri"];

                    logger.LogWarning("Retrying call to {uri} after {time} ms ({retryCount}, reason: {reason})",
                        uri, timestamp, retryCount, reason);
                });

            var circuitBreakerPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => statusCodesWorthRetrying.Contains(r.StatusCode))
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(30),
                    (result, timestamp, context) => logger.LogWarning("Broken circuit for {uri}", context["uri"]),
                    context => logger.LogInformation("Closed circuit for {uri}", context["uri"]));

            _policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var uri = request.RequestUri!.ToString();
            var contextData = new Dictionary<string, object> { ["uri"] = uri };

            var result = await _policy.ExecuteAsync(async (c, ct) => await base.SendAsync(request, ct),
                contextData, cancellationToken);

            return result;
        }
    }
}

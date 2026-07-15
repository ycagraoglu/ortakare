using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ortakare.IntegrationTests.Logging;

public sealed class RequestLoggingTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public RequestLoggingTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Request_logging_does_not_include_sensitive_header_values(CancellationToken cancellationToken)
    {
        var loggerProvider = new CapturingLoggerProvider();

        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => services.AddSingleton<ILoggerProvider>(loggerProvider));
        }).CreateClient();

        const string participantToken = "participant-secret-value";
        const string authorization = "Bearer jwt-secret-value";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/public/events/unknown-gallery-token");
        request.Headers.TryAddWithoutValidation("X-Participant-Token", participantToken);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);

        await client.SendAsync(request, cancellationToken);

        var logs = string.Join(Environment.NewLine, loggerProvider.Messages);
        Assert.Contains("HTTP request completed", logs);
        Assert.DoesNotContain(participantToken, logs);
        Assert.DoesNotContain(authorization, logs);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);
        public void Dispose() { }
    }

    private sealed class CapturingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Add(formatter(state, exception));
        }
    }
}

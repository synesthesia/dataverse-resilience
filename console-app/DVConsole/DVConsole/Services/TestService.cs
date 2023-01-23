using Microsoft.Extensions.Logging;

namespace DVConsole.Services;

internal class TestService : ITestInterface
{

    private readonly ILogger _logger;

    public TestService(ILogger<TestService> logger)
    {
        _logger = logger;
    }

    public void Foo()
    {
        _logger.LogInformation("Hello, World from Foo!");
    }
}
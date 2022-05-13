namespace FullStateDemo;

public class Another : IDisposable
{
    private readonly ILogger<Another> _logger;

    public Another(ILogger<Another> logger) => _logger = logger;

    public void Dispose()
    {
        _logger.LogInformation($"{this}({GetHashCode()}) disposed");
    }
}

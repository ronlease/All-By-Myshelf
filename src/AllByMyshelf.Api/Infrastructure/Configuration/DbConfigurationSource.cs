using Microsoft.Extensions.Configuration;

namespace AllByMyshelf.Api.Infrastructure.Configuration;

/// <summary>
/// Configuration source that loads settings from the app_settings database table.
/// </summary>
public class DbConfigurationSource(string connectionString) : IConfigurationSource
{
    private readonly string _connectionString = connectionString;

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new DbConfigurationProvider(_connectionString);
}

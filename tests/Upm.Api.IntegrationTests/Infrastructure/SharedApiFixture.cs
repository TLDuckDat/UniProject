namespace Upm.Api.IntegrationTests.Infrastructure;

/// <summary>Mọi lớp test API dùng chung MỘT container SQL Server + một API (khởi động một lần).</summary>
[CollectionDefinition(Name)]
public sealed class SharedApiFixture : ICollectionFixture<UpmApiFactory>
{
    public const string Name = "Api";
}

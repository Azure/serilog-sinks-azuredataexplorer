using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer;

public class AzureDataExplorerSinkOptionsExtensionsTests
{
    [Fact]
    public void GetKustoConnectionStringBuilder_AadApplicationKey_ReturnsExpectedConnectionString()
    {
        var options = new AzureDataExplorerSinkOptions
        {
            IngestionEndpointUri = "https://ingestion-endpoint-uri", DatabaseName = "database-name", TableName = "table-name"
        }.WithAadApplicationKey("applicationClientId", "applicationClientId", "authority");

        var kcsb = options.GetKustoConnectionStringBuilder();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
    }

    [Fact]
    public void GetKustoConnectionStringBuilder_AadApplicationCertificate_ReturnsExpectedConnectionString()
    {
        var options = new AzureDataExplorerSinkOptions
        {
            IngestionEndpointUri = "https://ingestion-endpoint-uri", DatabaseName = "database-name", TableName = "table-name"
        }.WithAadApplicationCertificate("applicationClientId", GetDummyCert(), "authority", false, "azureRegion");

        var kcsb = options.GetKustoConnectionStringBuilder();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
    }

    [Fact]
    public void GetKustoConnectionStringBuilder_AadApplicationSubjectName_ReturnsExpectedConnectionString()
    {
        var options = new AzureDataExplorerSinkOptions
        {
            IngestionEndpointUri = "https://ingestion-endpoint-uri", DatabaseName = "database-name", TableName = "table-name"
        }.WithAadApplicationSubjectName("applicationClientId", "applicationCertificateSubjectName", "authority",
            "azureRegion");

        var kcsb = options.GetKustoConnectionStringBuilder();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
    }

    [Fact]
    public void GetKustoConnectionStringBuilder_AadApplicationThumbprint_ReturnsExpectedConnectionString()
    {
        var options = new AzureDataExplorerSinkOptions
        {
            IngestionEndpointUri = "https://ingestion-endpoint-uri", DatabaseName = "database-name", TableName = "table-name"
        }.WithAadApplicationThumbprint("applicationClientId", "appCertThumbprint", "authority");

        var kcsb = options.GetKustoConnectionStringBuilder();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
    }

    [Fact]
    public void GetKustoConnectionStringBuilder_AadApplicationToken_ReturnsExpectedConnectionString()
    {
        var options = new AzureDataExplorerSinkOptions
        {
            IngestionEndpointUri = "https://ingestion-endpoint-uri", DatabaseName = "database-name", TableName = "table-name"
        }.WithAadApplicationToken("applicationToken");

        var kcsb = options.GetKustoConnectionStringBuilder();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
    }

    [Fact]
    public void GetKustoConnectionStringBuilder_AadUserPrompt_ReturnsExpectedConnectionString()
    {
        var options = new AzureDataExplorerSinkOptions
        {
            IngestionEndpointUri = "https://ingestion-endpoint-uri", DatabaseName = "database-name", TableName = "table-name"
        }.WithAadUserToken("userToken");

        var kcsb = options.GetKustoConnectionStringBuilder();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
    }

    [Fact]
    public void GetKustoConnectionStringBuilder_AadSystemManagedIdentity_ReturnsExpectedConnectionString()
    {
        var options = new AzureDataExplorerSinkOptions
        {
            IngestionEndpointUri = "https://ingestion-endpoint-uri", DatabaseName = "database-name", TableName = "table-name"
        }.WithAadSystemAssignedManagedIdentity();

        var kcsb = options.GetKustoConnectionStringBuilder();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
    }

    [Fact]
    public void GetKustoConnectionStringBuilder_AadUserManagedIdentity_ReturnsExpectedConnectionString()
    {
        var options = new AzureDataExplorerSinkOptions
        {
            IngestionEndpointUri = "https://ingestion-endpoint-uri", DatabaseName = "database-name", TableName = "table-name"
        }.WithAadUserAssignedManagedIdentity(Guid.NewGuid().ToString());

        var kcsb = options.GetKustoConnectionStringBuilder();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
    }

    private X509Certificate2 GetDummyCert()
    {
        using (var rsa = RSA.Create())
        {
            var request = new CertificateRequest("CN=DummyCertificate", rsa, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));

            var certificate =
                request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
            var dummyCertificate = new X509Certificate2(certificate.Export(X509ContentType.Pfx));
            return dummyCertificate;
        }
    }
}

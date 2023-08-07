using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer;

public class AzureDataExplorerSinkOptionsExtensionsTests
{
    [Fact]
    public void GetKustoConnectionStringBuilder_AadSystemManagedIdentity_ReturnsExpectedConnectionString()
    {
        var options = new AzureDataExplorerSinkOptions
        {
            ConnectionString = "https://ingestion-endpoint-uri",
            DatabaseName = "database-name",
            TableName = "table-name",
            ManagedIdentity = "system"
        };
        var kcsb = options.GetIngestKcsb();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
        Assert.Equal("system", kcsb.EmbeddedManagedIdentity);
    }

    [Fact]
    public void GetKustoConnectionStringBuilder_AadUserManagedIdentity_ReturnsExpectedConnectionString()
    {
        var userManagedIdentity = Guid.NewGuid().ToString();
        var options = new AzureDataExplorerSinkOptions
        {
            ConnectionString = "https://ingestion-endpoint-uri",
            DatabaseName = "database-name",
            TableName = "table-name",
            ManagedIdentity = userManagedIdentity
        };
        var kcsb = options.GetIngestKcsb();
        var connectionString = kcsb.ToString();
        Assert.NotNull(kcsb);
        Assert.NotEmpty(connectionString);
        Assert.Equal(userManagedIdentity, kcsb.EmbeddedManagedIdentity);
    }


    [Theory]
    [InlineData($"Data Source=https://dummy-test.eastus.dev.kusto.windows.net;Database=NetDefaultDB;Fed=True;Authority Id=f2552aa9-174c-4993-b787-2d1ec77ed3b0")]
    [InlineData($"Data Source=https://dummy-test.eastus.dev.kusto.windows.net;Database=NetDefaultDB;Fed=True;Authority Id=f2552aa9-174c-4993-b787-2d1ec77ed3b0;User ID=john.doe@contoso.com")]
    [InlineData($"Data Source=https://dummy-test.eastus.dev.kusto.windows.net;Database=NetDefaultDB;Fed=True;AppClientId=d1d32ae3-0730-4202-a879-721d6d286d3b;AppKey=!!@#a536f-a61b-42e7-b1ab-f72461504bd3;Authority Id=f2552aa9-174c-4993-b787-2d1ec77ed3b0")]
    [InlineData($"Data Source=https://dummy-test.eastus.dev.kusto.windows.net;Database=NetDefaultDB;Fed=True;UserToken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXUyJ9.eyJjb21wYW55IjoiRnV0dXJlRWQiLCJzdWIiOjEsImlzcyI6Imh0dHA6XC9cL2Z1dHVyZWVkLmRldlwvYXBpXC92MVwvc3R1ZGVudFwvbG9naW5cL3VzZXJuYW1lIiwiaWF0IjoiMTQyNzQyNjc3MSIsImV4cCI6IjE0Mjc0MzAzNzEiLCJuYmYiOiIxNDI3NDI2NzcxIiwianRpIjoiNmFlZDQ3MGFiOGMxYTk0MmE0MTViYTAwOTBlMTFlZTUifQ.MmM2YTUwMjEzYTE0OGNhNjk5Y2Y2MjEwZDdkN2Y1OTQ2NWVhZTdmYmI4OTA5YmM1Y2QwYTMzZjUwNTgwY2Y0MQ")]
    [InlineData($"Data Source=https://dummy-test.eastus.dev.kusto.windows.net;Database=NetDefaultDB;Fed=True;AppToken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXUyJ9.eyJjb21wYW55IjoiRnV0dXJlRWQiLCJzdWIiOjEsImlzcyI6Imh0dHA6XC9cL2Z1dHVyZWVkLmRldlwvYXBpXC92MVwvc3R1ZGVudFwvbG9naW5cL3VzZXJuYW1lIiwiaWF0IjoiMTQyNzQyNjc3MSIsImV4cCI6IjE0Mjc0MzAzNzEiLCJuYmYiOiIxNDI3NDI2NzcxIiwianRpIjoiNmFlZDQ3MGFiOGMxYTk0MmE0MTViYTAwOTBlMTFlZTUifQ.MmM2YTUwMjEzYTE0OGNhNjk5Y2Y2MjEwZDdkN2Y1OTQ2NWVhZTdmYmI4OTA5YmM1Y2QwYTMzZjUwNTgwY2Y0MQ")]
    public void GetKustoConnectionStringBuilder_ConnectionString_ReturnsExpectedConnectionString(string connectionString)
    {
        var options = new AzureDataExplorerSinkOptions
        {
            ConnectionString = connectionString
        };
        var ingestKcsb = options.GetIngestKcsb();
        var parsedIngestConnectionString = ingestKcsb.ToString();
        Assert.NotNull(ingestKcsb);
        Assert.NotEmpty(parsedIngestConnectionString);
        var engineKcsb = options.GetEngineKcsb();
        var parsedEngineConnectionString = engineKcsb.ToString();
        Assert.NotNull(engineKcsb);
        Assert.NotEmpty(parsedEngineConnectionString);
        Assert.Equal("dummy-test.eastus.dev.kusto.windows.net", engineKcsb.Hostname);
        Assert.Equal("ingest-dummy-test.eastus.dev.kusto.windows.net", ingestKcsb.Hostname);
    }

    [Fact]
    public void GetKustoConnectionStringBuilder_CertificateAuth_ReturnsExpectedConnectionString()
    {
        /*
            Azure AD Application Subject and Issuer Authentication 	
            - Application Client Id
            - Application Certificate Subject Distinguished Name
            - Application Certificate Issuer Distinguished Name
            - Authority Id
            */
        var dummyCertificate = GetDummyCert();
        var subjectName = dummyCertificate.Subject;
        var issuerName = dummyCertificate.Issuer;
        var options = new AzureDataExplorerSinkOptions
        {
            ConnectionString = $"Data Source=https://dummy-test.eastus.dev.kusto.windows.net;Database=NetDefaultDB;Fed=True;Authority Id=f2552aa9-174c-4993-b787-2d1ec77ed3b0;AppClientId=a2552aa9-174c-4993-b787-2d1ec77ed3b0;Application Certificate Subject={subjectName};Application Certificate Issuer={issuerName}"
        };
        var engineKcsb = options.GetEngineKcsb();
        Assert.NotNull(engineKcsb);
        Assert.Equal(subjectName, engineKcsb.ApplicationCertificateSubjectDistinguishedName);
        Assert.Equal(issuerName, engineKcsb.ApplicationCertificateIssuerDistinguishedName);
    }

    private static X509Certificate2 GetDummyCert()
    {
        using var rsa = RSA.Create();
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

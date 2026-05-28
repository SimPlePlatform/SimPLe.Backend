using FluentAssertions;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SimPle.Application.Common.Options;
using SimPle.Infrastructure.Storage;

namespace SimPle.UnitTests.Profiles;

public sealed class StorageOptionsTests
{
    [Fact]
    public void MinioLocalConfig_BindsExpectedS3CompatibleOptions()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "S3Compatible",
            ["Storage:BucketName"] = "simple-profile-assets-dev",
            ["Storage:Region"] = "us-east-1",
            ["Storage:ServiceUrl"] = "http://localhost:9000",
            ["Storage:AccessKey"] = "simpleadmin",
            ["Storage:SecretKey"] = "simpleadmin123",
            ["Storage:ProfilePrefix"] = "profile-assets",
            ["Storage:ForcePathStyle"] = "true",
            ["Storage:UploadUrlExpiryMinutes"] = "5",
            ["Storage:ReadUrlExpiryMinutes"] = "30",
        });

        options.Provider.Should().Be("S3Compatible");
        options.BucketName.Should().Be("simple-profile-assets-dev");
        options.ServiceUrl.Should().Be("http://localhost:9000");
        options.ForcePathStyle.Should().BeTrue();
        options.UploadUrlExpiryMinutes.Should().Be(5);
        options.ReadUrlExpiryMinutes.Should().Be(30);

        var config = S3FileStorageService.BuildClientConfig(options);
        config.ServiceURL.TrimEnd('/').Should().Be("http://localhost:9000");
        config.ForcePathStyle.Should().BeTrue();
        config.AuthenticationRegion.Should().Be("us-east-1");
    }

    [Fact]
    public void AwsProductionConfig_AllowsEmptyServiceUrlAndVirtualHostedStyle()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "AWS",
            ["Storage:BucketName"] = "simpleplatform-profile-assets-prod",
            ["Storage:Region"] = "eu-west-1",
            ["Storage:ServiceUrl"] = "",
            ["Storage:ProfilePrefix"] = "profile-assets",
            ["Storage:ForcePathStyle"] = "false",
            ["Storage:UploadUrlExpiryMinutes"] = "5",
            ["Storage:ReadUrlExpiryMinutes"] = "30",
        });

        options.Provider.Should().Be("AWS");
        options.BucketName.Should().Be("simpleplatform-profile-assets-prod");
        options.ServiceUrl.Should().BeEmpty();
        options.ForcePathStyle.Should().BeFalse();

        var config = S3FileStorageService.BuildClientConfig(options);
        config.ServiceURL.Should().BeNull();
        config.ForcePathStyle.Should().BeFalse();
        config.RegionEndpoint.SystemName.Should().Be("eu-west-1");
    }

    [Fact]
    public async Task StorageService_CreatesPresignedPutAndReadUrlsThroughS3Client()
    {
        var options = LocalOptions();
        var client = Substitute.For<IAmazonS3>();
        client.GetPreSignedURL(Arg.Any<GetPreSignedUrlRequest>())
            .Returns("http://localhost:9000/simple-profile-assets-dev/profile-assets/file.png?signature=test");
        var service = new S3FileStorageService(options, client);

        var putUrl = await service.CreatePresignedPutUrlAsync("profile-assets/file.png", "image/png", TimeSpan.FromMinutes(5));
        var readUrl = await service.CreatePresignedReadUrlAsync("profile-assets/file.png", TimeSpan.FromMinutes(30));

        putUrl.Should().Contain("signature=test");
        readUrl.Should().Contain("signature=test");
        client.Received(1).GetPreSignedURL(Arg.Is<GetPreSignedUrlRequest>(r =>
            r.BucketName == options.BucketName &&
            r.Key == "profile-assets/file.png" &&
            r.Verb == HttpVerb.PUT &&
            r.ContentType == "image/png"));
        client.Received(1).GetPreSignedURL(Arg.Is<GetPreSignedUrlRequest>(r =>
            r.BucketName == options.BucketName &&
            r.Key == "profile-assets/file.png" &&
            r.Verb == HttpVerb.GET));
    }

    [Fact]
    public async Task StorageService_ObjectExists_ReturnsTrueWhenMetadataExists()
    {
        var options = LocalOptions();
        var client = Substitute.For<IAmazonS3>();
        client.GetObjectMetadataAsync(Arg.Any<GetObjectMetadataRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetObjectMetadataResponse());
        var service = new S3FileStorageService(options, client);

        var exists = await service.ObjectExistsAsync("profile-assets/file.png");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task StorageService_DeleteObject_UsesConfiguredBucketAndKey()
    {
        var options = LocalOptions();
        var client = Substitute.For<IAmazonS3>();
        client.DeleteObjectAsync(Arg.Any<DeleteObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse());
        var service = new S3FileStorageService(options, client);

        await service.DeleteObjectAsync("profile-assets/file.png");

        await client.Received(1).DeleteObjectAsync(Arg.Is<DeleteObjectRequest>(r =>
            r.BucketName == options.BucketName && r.Key == "profile-assets/file.png"), Arg.Any<CancellationToken>());
    }

    private static StorageOptions Bind(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var options = new StorageOptions();
        configuration.GetSection(StorageOptions.SectionName).Bind(options);
        return options;
    }

    private static StorageOptions LocalOptions() => new()
    {
        Provider = "S3Compatible",
        BucketName = "simple-profile-assets-dev",
        Region = "us-east-1",
        ServiceUrl = "http://localhost:9000",
        AccessKey = "simpleadmin",
        SecretKey = "simpleadmin123",
        ProfilePrefix = "profile-assets",
        ForcePathStyle = true,
        UploadUrlExpiryMinutes = 5,
        ReadUrlExpiryMinutes = 30,
    };
}

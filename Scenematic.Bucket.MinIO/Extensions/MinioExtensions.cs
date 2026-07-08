using Microsoft.Extensions.DependencyInjection;
using Minio;
using Scenematic.Bucket.MinIO.Options;

namespace Scenematic.Bucket.MinIO.Extensions;

public static class MinioExtensions
{
    public static void UseScenematicMinio(this IServiceCollection services, MinioBucketOptions minioOptions)
    {
        services.AddMinio(client => client
            .WithEndpoint(minioOptions.Endpoint)
            .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
            .WithSSL(minioOptions.UseSSL)
            .Build()
        );

        services.AddScoped<IBucket, MinioBucket>();
    }
}
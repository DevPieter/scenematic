using Microsoft.Extensions.DependencyInjection;
using Minio;
using Scenematic.Bucket.Buckets;
using Scenematic.Bucket.MinIO.Buckets;
using Scenematic.Bucket.MinIO.Options;

namespace Scenematic.Bucket.MinIO.Extensions;

public static class BuilderExtensions
{
    public static void UseScenematicMinIO(this IServiceCollection services, MinioBucketOptions minioOptions)
    {
        services.AddMinio(client => client
            .WithEndpoint(minioOptions.Endpoint)
            .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
            .WithSSL(minioOptions.UseSSL)
            .Build()
        );

        services.AddScoped<ICoverBucket, CoverBucket>();
    }
}
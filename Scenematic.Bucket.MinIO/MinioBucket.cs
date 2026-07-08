using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Scenematic.Bucket.MinIO.Extensions;
using Scenematic.Bucket.Models.PutItem;
using Scenematic.Bucket.Models.RemoveItem;
using Scenematic.Common.Models.Common;

namespace Scenematic.Bucket.MinIO;

public class MinioBucket(
    IMinioClient minioClient,
    ILogger<MinioBucket> logger
) : IBucket
{
    public async Task<ResultObject<PutItemResult>> UploadItemAsync(
        string localPath,
        PutItemArgs args,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            args.EnsureValid();
        }
        catch (ArgumentException ex)
        {
            logger.LogError(
                "Validation failed for PutItemArgs. Bucket: {Bucket}, Path: {Path}, FileName: {FileName}, Extension: {Extension}, ContentType: {ContentType}. Error: {Error}",
                args.Bucket,
                args.Path,
                args.FileName,
                args.Extension,
                args.ContentType,
                ex.Message
            );

            return ResultObject<PutItemResult>.Validation(ex.Message);
        }

        var fileInfo = new FileInfo(localPath);
        if (!fileInfo.Exists)
        {
            logger.LogError(
                "Local file does not exist. Path: {LocalPath}",
                localPath
            );

            return ResultObject<PutItemResult>.NotFound($"Local file does not exist at path: {localPath}");
        }

        if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
        {
            logger.LogError(
                "Local path is a directory, not a file. Path: {LocalPath}",
                localPath
            );

            return ResultObject<PutItemResult>.Validation($"Local path is a directory, not a file: {localPath}");
        }

        try
        {
            var objectName = args.ToObjectName();

            await minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(args.Bucket)
                    .WithObject(objectName)
                    .WithFileName(localPath)
                    .WithContentType(args.ContentType),
                cancellationToken
            );

            logger.LogInformation(
                "Successfully uploaded object to MinIO. Bucket: {Bucket}, ObjectName: {ObjectName}",
                args.Bucket,
                objectName
            );

            return ResultObject<PutItemResult>.Success(PutItemResult.FromArgs(
                args: args,
                objectName: objectName
            ));
        }
        catch (AuthorizationException ex)
        {
            logger.LogError(
                "Authorization failed when uploading object to MinIO. Bucket: {Bucket}. Error: {Error}",
                args.Bucket,
                ex.Message
            );

            return ResultObject<PutItemResult>.Unauthorized(
                $"Authorization failed for bucket: {args.Bucket}. Error: {ex.Message}"
            );
        }
        catch (InvalidBucketNameException ex)
        {
            logger.LogError(
                "Invalid bucket name when uploading object to MinIO. Bucket: {Bucket}. Error: {Error}",
                args.Bucket,
                ex.Message
            );

            return ResultObject<PutItemResult>.Validation(
                $"Invalid bucket name: {args.Bucket}. Error: {ex.Message}"
            );
        }
        catch (InvalidObjectNameException ex)
        {
            logger.LogError(
                "Invalid object name when uploading object to MinIO. Bucket: {Bucket}, ObjectName: {ObjectName}. Error: {Error}",
                args.Bucket,
                args.ToObjectName(),
                ex.Message
            );

            return ResultObject<PutItemResult>.Validation(
                $"Invalid object name: {args.ToObjectName()}. Error: {ex.Message}"
            );
        }
        catch (BucketNotFoundException ex)
        {
            logger.LogError(
                "Bucket not found when uploading object to MinIO. Bucket: {Bucket}. Error: {Error}",
                args.Bucket,
                ex.Message
            );

            return ResultObject<PutItemResult>.NotFound(
                $"Bucket not found: {args.Bucket}. Error: {ex.Message}"
            );
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(
                "Local file not found when uploading object to MinIO. LocalPath: {LocalPath}. Error: {Error}",
                localPath,
                ex.Message
            );

            return ResultObject<PutItemResult>.NotFound(
                $"Local file not found at path: {localPath}. Error: {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Failed to upload object to MinIO. Bucket: {Bucket}, Path: {Path}, FileName: {FileName}, Extension: {Extension}, ContentType: {ContentType}. Error: {Error}",
                args.Bucket,
                args.Path,
                args.FileName,
                args.Extension,
                args.ContentType,
                ex.Message
            );

            return ResultObject<PutItemResult>.InternalError(ex.Message);
        }
    }

    public async Task<Result> DeleteItemAsync(RemoveItemArgs args, CancellationToken cancellationToken = default)
    {
        try
        {
            await minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(args.Bucket)
                    .WithObject(args.ObjectName),
                cancellationToken
            );

            logger.LogInformation(
                "Successfully deleted object from MinIO. Bucket: {Bucket}, ObjectName: {ObjectName}",
                args.Bucket,
                args.ObjectName
            );

            return Result.Success();
        }
        catch (AuthorizationException ex)
        {
            logger.LogError(
                "Authorization failed when deleting object from MinIO. Bucket: {Bucket}, ObjectName: {ObjectName}. Error: {Error}",
                args.Bucket,
                args.ObjectName,
                ex.Message
            );

            return Result.Unauthorized(ex.Message);
        }
        catch (InvalidBucketNameException ex)
        {
            logger.LogError(
                "Invalid bucket name when deleting object from MinIO. Bucket: {Bucket}. Error: {Error}",
                args.Bucket,
                ex.Message
            );

            return Result.Validation($"Invalid bucket name: {args.Bucket}. Error: {ex.Message}");
        }
        catch (InvalidObjectNameException ex)
        {
            logger.LogError(
                "Invalid object name when deleting object from MinIO. Bucket: {Bucket}, ObjectName: {ObjectName}. Error: {Error}",
                args.Bucket,
                args.ObjectName,
                ex.Message
            );

            return Result.Validation($"Invalid object name: {args.ObjectName}. Error: {ex.Message}");
        }
        catch (BucketNotFoundException ex)
        {
            logger.LogError(
                "Bucket not found when deleting object from MinIO. Bucket: {Bucket}. Error: {Error}",
                args.Bucket,
                ex.Message
            );

            return Result.NotFound($"Bucket not found: {args.Bucket}. Error: {ex.Message}");
        }
        catch (ObjectNotFoundException ex)
        {
            logger.LogError(
                "Object not found when deleting object from MinIO. Bucket: {Bucket}, ObjectName: {ObjectName}. Error: {Error}",
                args.Bucket,
                args.ObjectName,
                ex.Message
            );

            return Result.NotFound(
                $"Object not found: {args.ObjectName} in bucket: {args.Bucket}. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Failed to delete object from MinIO. Bucket: {Bucket}, ObjectName: {ObjectName}. Error: {Error}",
                args.Bucket,
                args.ObjectName,
                ex.Message
            );

            return Result.InternalError(ex.Message);
        }
    }
}
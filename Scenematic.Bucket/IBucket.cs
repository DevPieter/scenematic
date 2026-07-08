using Scenematic.Bucket.Models.PutItem;
using Scenematic.Bucket.Models.RemoveItem;
using Scenematic.Common.Models.Common;

namespace Scenematic.Bucket;

public interface IBucket
{
    Task<ResultObject<PutItemResult>> UploadItemAsync(
        string localPath,
        PutItemArgs args,
        CancellationToken cancellationToken = default
    );

    Task<Result> DeleteItemAsync(
        RemoveItemArgs args,
        CancellationToken cancellationToken = default
    );
}
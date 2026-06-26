using Scenematic.Bucket.Models.Covers;
using Scenematic.Bucket.Models;
using Scenematic.Common.Models.Common;

namespace Scenematic.Bucket.Buckets;

public interface ICoverBucket
{
    Task<ResultObject<PutCoverResult>> PutCoverAsync(PutCoverArgs args, CancellationToken cancellationToken = default);
}
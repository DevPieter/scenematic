namespace Scenematic.Bucket.Models.PutItem;

public record PutItemArgs(
    string Bucket,
    string Path,
    string FileName,
    string Extension,
    string ContentType
);
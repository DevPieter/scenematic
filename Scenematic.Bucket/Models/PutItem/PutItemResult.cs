namespace Scenematic.Bucket.Models.PutItem;

public record PutItemResult(
    string Bucket,
    string Path,
    string FileName,
    string Extension,
    string ContentType,
    string ObjectName
)
{
    public string Path => $"{Bucket}/{ObjectName}";

    public static PutItemResult FromArgs(PutItemArgs args, string objectName) => new(
        Bucket: args.Bucket,
        Path: args.Path,
        FileName: args.FileName,
        Extension: args.Extension,
        ContentType: args.ContentType,
        ObjectName: objectName
    );
}
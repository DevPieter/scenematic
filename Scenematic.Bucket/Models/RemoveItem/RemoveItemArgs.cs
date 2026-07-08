namespace Scenematic.Bucket.Models.RemoveItem;

public record RemoveItemArgs(
    string Bucket,
    string ObjectName
);
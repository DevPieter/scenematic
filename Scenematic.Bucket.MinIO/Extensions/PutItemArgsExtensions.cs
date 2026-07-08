using System.Text.RegularExpressions;
using Scenematic.Bucket.Models.PutItem;

namespace Scenematic.Bucket.MinIO.Extensions;

public static partial class PutItemArgsExtensions
{
    // my-bucket-name, my.bucket.name, mybucketname, my-bucket-name-123
    [GeneratedRegex(@"^(?=.{3,63}$)[a-z0-9]+(?:[.-][a-z0-9]+)*$")]
    private static partial Regex BucketRegex();

    // Linux-style path: /foo/bar, foo/bar, foo/bar/baz
    [GeneratedRegex(@"^(?:/?[A-Za-z0-9._-]+)+(?:/[A-Za-z0-9._-]+)*$")]
    private static partial Regex PathRegex();

    // Linux-style filename: file, my-file_01, image.v1
    [GeneratedRegex(@"^[A-Za-z0-9._-]+$")]
    private static partial Regex FileNameRegex();

    // .jpg, .png, .txt, .pdf
    [GeneratedRegex(@"^\.[A-Za-z0-9]+$")]
    private static partial Regex ExtensionRegex();

    // image/jpeg, image/png, text/plain, application/pdf
    [GeneratedRegex(@"^[a-z]+/[a-z0-9.+-]+$")]
    private static partial Regex ContentTypeRegex();

    public static void EnsureValid(this PutItemArgs args)
    {
        EnsureNonWhiteSpaceValidString(args.Bucket, "Bucket name is invalid.", BucketRegex());
        EnsureNonWhiteSpaceValidString(args.Path, "Path is invalid.", PathRegex());
        EnsureNonWhiteSpaceValidString(args.FileName, "File name is invalid.", FileNameRegex());
        EnsureNonWhiteSpaceValidString(args.Extension, "Extension is invalid.", ExtensionRegex());
        EnsureNonWhiteSpaceValidString(args.ContentType, "Content type is invalid.", ContentTypeRegex());
    }

    public static string ToObjectName(this PutItemArgs args)
    {
        var path = args.Path.Trim('/');
        var fileName = args.FileName;
        var extension = args.Extension;

        return $"{path}/{fileName}{extension}";
    }

    private static void EnsureNonWhiteSpaceValidString(string input, string message, Regex regex)
    {
        if (string.IsNullOrWhiteSpace(input) || !regex.IsMatch(input))
            throw new ArgumentException(message, nameof(input));
    }
}
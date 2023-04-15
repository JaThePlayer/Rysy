namespace Rysy.Extensions;

public static class StreamExt {

    /// <summary>
    /// Reads all characters from the stream from the current position to the end of the stream.
    /// </summary>
    public static string ReadAllText(this Stream stream) {
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads all characters from the stream from the current position to the end of the stream.
    /// </summary>
    public static byte[] ReadAllBytes(this Stream stream) {
        MemoryStream ms = stream.Length <= int.MaxValue ? new((int)stream.Length) : new();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}

namespace Rysy.Extensions;

public static class StreamExt {
    public static string ReadAllText(this Stream stream) {
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}

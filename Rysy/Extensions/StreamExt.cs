namespace Rysy.Extensions;

public static class StreamExt {

    extension(Stream stream)
    {
        /// <summary>
        /// Reads all characters from the stream from the current position to the end of the stream.
        /// </summary>
        public string ReadAllText() {
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        /// <summary>
        /// Reads all characters from the stream from the current position to the end of the stream.
        /// </summary>
        public byte[] ReadAllBytes() {
            MemoryStream ms = stream.Length <= int.MaxValue ? new((int)stream.Length) : new();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}

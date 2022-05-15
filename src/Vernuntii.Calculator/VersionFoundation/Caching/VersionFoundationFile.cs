﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using Vernuntii.SemVer.Json;
using Teronis.IO.FileLocking;

namespace Vernuntii.VersionFoundation.Caching
{
    internal sealed class VersionFoundationFile<T> : IDisposable, IVersionFoundationWriter<T>
        where T : class
    {
        public readonly static FileStreamLocker FileStreamLocker = new FileStreamLocker(new LockFileSystem());

        private static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions() {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        static VersionFoundationFile() => SerializerOptions.Converters.Add(SemVer.Json.System.VersionStringJsonConverter.Default);

        public static T ReadPresentationFoundation(FileStream stream)
        {
            stream.Position = 0;

            return JsonSerializer.Deserialize<T>(stream, SerializerOptions)
                ?? throw new JsonException($"A non-null serialized type of {typeof(T).FullName} was expected");
        }

        private readonly FileStream _stream;

        public VersionFoundationFile(string jsonFilePath, int lockTimeout)
        {
            _stream = FileStreamLocker.WaitUntilAcquired(jsonFilePath, lockTimeout)
                ?? throw new TimeoutException("Locking the cache file has been aborted due to timeout");
        }

        /// <inheritdoc/>
        public void WriteVersionFoundation(T value)
        {
            if (_stream.Length != 0) {
                _stream.SetLength(0);
                _stream.Flush();
            }

            JsonSerializer.Serialize(_stream, value, SerializerOptions);
        }

        public bool TryReadPresentationFoundation([NotNullWhen(true)] out T? value)
        {
            if (_stream.Length == 0) {
                value = null;
                return false;
            }

            value = ReadPresentationFoundation(_stream);
            return true;
        }

        public void Dispose() => _stream.Dispose();
    }
}

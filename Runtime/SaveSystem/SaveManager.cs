using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JadedBelles.Util.SaveSystem
{
    internal enum SaveWriteStage { TemporaryFlushed, BackupPrepared }

    /// <summary>
    /// Native-platform JSON persistence. Share ONE instance per directory/slot.
    /// Owns no scene objects, accounts, cloud provider, or game-specific data.
    /// </summary>
    public sealed class SaveManager<T> where T : class
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly SaveDefinition<T> _definition;
        private readonly SaveCodec<T> _codec;
        private readonly string _directory;
        private readonly string _primary;
        private readonly string _backup;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Action<SaveWriteStage> _writeCheckpoint;
        private bool _writeAllowed;

        /// <param name="directory">Dedicated absolute directory, resolved on the Unity main thread.</param>
        /// <param name="slot">ASCII letters, digits, hyphens and underscores only.</param>
        public SaveManager(string directory, string slot, SaveDefinition<T> definition)
            : this(directory, slot, definition, null) { }

        internal SaveManager(string directory, string slot, SaveDefinition<T> definition,
            Action<SaveWriteStage> writeCheckpoint)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new PlatformNotSupportedException("SaveManager requires native filesystem/thread support. Use a WebGL adapter.");
#else
            if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathRooted(directory))
                throw new ArgumentException("An absolute save directory is required.", nameof(directory));
            ValidatePathSegment(slot, nameof(slot));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _codec = new SaveCodec<T>(definition);
            _directory = Path.GetFullPath(directory);
            _primary = Path.Combine(_directory, slot + ".json");
            _backup = _primary + ".bak";
            _writeCheckpoint = writeCheckpoint;
#endif
        }

        internal static void ValidatePathSegment(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
                throw new ArgumentException("Path segment must contain 1 to 64 characters.", name);
            foreach (char character in value)
                if (!(character >= 'a' && character <= 'z') && !(character >= 'A' && character <= 'Z') &&
                    !(character >= '0' && character <= '9') && character != '-' && character != '_')
                    throw new ArgumentException("Only ASCII letters, digits, hyphens and underscores are allowed.", name);
            // Also reject reserved Windows device names so slots are portable.
            string upper = value.ToUpperInvariant();
            if (upper == "CON" || upper == "PRN" || upper == "AUX" || upper == "NUL" ||
                (upper.Length == 4 && (upper.StartsWith("COM", StringComparison.Ordinal) ||
                 upper.StartsWith("LPT", StringComparison.Ordinal)) && upper[3] >= '1' && upper[3] <= '9'))
                throw new ArgumentException("Reserved device name.", name);
        }

        public async Task<SaveLoadResult<T>> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _writeAllowed = false;
                var result = await Task.Run(() => LoadCore(cancellationToken), cancellationToken).ConfigureAwait(false);
                _writeAllowed = result.Success;
                return result;
            }
            finally { _gate.Release(); }
        }

        /// <summary>
        /// Pass a detached snapshot and do not modify it until completion.
        /// All validation, serialization and file work run off the Unity main thread.
        /// </summary>
        public async Task SaveAsync(T snapshot, CancellationToken cancellationToken = default)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_writeAllowed)
                    throw new InvalidOperationException("Load successfully before saving. Existing data is protected.");
                await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string json = _codec.Encode(snapshot);
                    byte[] bytes = Utf8.GetBytes(json);
                    if (bytes.Length > _definition.MaximumBytes)
                        throw new InvalidSaveException("Save exceeds configured byte limit.");
                    _codec.Decode(json); // Verify required fields and depth with the same reader.
                    await WriteCoreAsync(bytes, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            finally { _gate.Release(); }
        }

        private SaveLoadResult<T> LoadCore(CancellationToken cancellationToken)
        {
            var errors = new List<Exception>();
            bool corrupt = false;
            foreach (string path in new[] { _primary, _backup })
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var data = Read(path);
                    cancellationToken.ThrowIfCancellationRequested();
                    return new SaveLoadResult<T>(path == _primary ? SaveLoadStatus.Loaded :
                        SaveLoadStatus.RecoveredBackup, data, errors);
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                catch (InvalidSaveException exception) { corrupt = true; errors.Add(exception); }
                catch (Exception exception) when (exception is SaveCompatibilityException ||
                    exception is IOException || exception is UnauthorizedAccessException)
                {
                    errors.Add(exception);
                    return new SaveLoadResult<T>(SaveLoadStatus.Failed, null, errors);
                }
            }
            if (corrupt) return new SaveLoadResult<T>(SaveLoadStatus.Failed, null, errors);
            cancellationToken.ThrowIfCancellationRequested();
            var defaults = _definition.CreateDefault();
            _definition.ValidateData(defaults);
            return new SaveLoadResult<T>(SaveLoadStatus.NewGame, defaults, errors);
        }

        private T Read(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > _definition.MaximumBytes)
                    throw new InvalidSaveException("Save exceeds configured byte limit: " + path);
                try
                {
                    using (var reader = new StreamReader(stream, Utf8, false))
                        return _codec.Decode(reader.ReadToEnd());
                }
                catch (DecoderFallbackException exception)
                {
                    throw new InvalidSaveException("Invalid UTF-8: " + path, exception);
                }
            }
        }

        private async Task WriteCoreAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_directory);
            string temporary = _primary + "." + Guid.NewGuid().ToString("N") + ".tmp";
            string backupTemporary = temporary + ".bak";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, 65536, FileOptions.Asynchronous))
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    stream.Flush(true); // Potentially blocking, but still on the worker.
                }
                _writeCheckpoint?.Invoke(SaveWriteStage.TemporaryFlushed);
                cancellationToken.ThrowIfCancellationRequested();
                bool primaryExists = false;
                bool primaryValid = false;
                try { Read(_primary); primaryExists = primaryValid = true; }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                catch (InvalidSaveException)
                {
                    primaryExists = true;
                    File.Copy(_primary, _primary + ".corrupt-" + Guid.NewGuid().ToString("N"));
                }
                if (primaryValid)
                {
                    File.Copy(_primary, backupTemporary);
                    using (var stream = new FileStream(backupTemporary, FileMode.Open, FileAccess.Write, FileShare.None))
                        stream.Flush(true);
                    if (File.Exists(_backup)) File.Replace(backupTemporary, _backup, null);
                    else File.Move(backupTemporary, _backup);
                }
                _writeCheckpoint?.Invoke(SaveWriteStage.BackupPrepared);
                cancellationToken.ThrowIfCancellationRequested();
                // No cancellation after publication starts: committed means success.
                if (primaryExists) File.Replace(temporary, _primary, null);
                else File.Move(temporary, _primary);
            }
            finally { TryDelete(temporary); TryDelete(backupTemporary); }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

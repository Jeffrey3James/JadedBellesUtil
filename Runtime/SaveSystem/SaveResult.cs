using System;
using System.Collections.Generic;

namespace JadedBelles.Util.SaveSystem
{
    public enum SaveLoadStatus { NewGame, Loaded, RecoveredBackup, Failed }

    /// <summary>A failed load never supplies default data or enables writes.</summary>
    public sealed class SaveLoadResult<T> where T : class
    {
        public SaveLoadStatus Status { get; }
        public T Data { get; }
        public IReadOnlyList<Exception> Errors { get; }
        public bool Success => Status != SaveLoadStatus.Failed;

        internal SaveLoadResult(SaveLoadStatus status, T data, List<Exception> errors)
        {
            Status = status;
            Data = data;
            Errors = errors.AsReadOnly();
        }
    }

    /// <summary>Malformed JSON or a payload that fails the game's validation.</summary>
    public sealed class InvalidSaveException : Exception
    {
        public InvalidSaveException(string message) : base(message) { }
        public InvalidSaveException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Readable but incompatible data. Never recover by silently downgrading it.</summary>
    public sealed class SaveCompatibilityException : Exception
    {
        public SaveCompatibilityException(string message) : base(message) { }
    }
}

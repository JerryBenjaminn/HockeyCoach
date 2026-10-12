using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>Thrown when a match JSON cannot be replayed: invalid JSON, unsupported format version or missing or inconsistent data.</summary>
    public sealed class ReplayFormatException : Exception
    {
        /// <summary>Creates the exception with a message.</summary>
        public ReplayFormatException(string message)
            : base(message)
        {
        }

        /// <summary>Creates the exception with a message and the underlying error.</summary>
        public ReplayFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

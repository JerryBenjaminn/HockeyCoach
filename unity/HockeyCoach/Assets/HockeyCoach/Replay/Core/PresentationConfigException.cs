using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Thrown when <c>data/replay.json</c> (D-088 B) cannot be used: invalid JSON, wrong <c>schemaVersion</c>, a missing or
    /// unknown key or a value outside its range. The message names the key path, e.g. <c>timing.actionCompression</c>.
    /// </summary>
    public sealed class PresentationConfigException : Exception
    {
        /// <summary>Creates the exception with a message.</summary>
        public PresentationConfigException(string message)
            : base(message)
        {
        }

        /// <summary>Creates the exception with a message and the underlying error.</summary>
        public PresentationConfigException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

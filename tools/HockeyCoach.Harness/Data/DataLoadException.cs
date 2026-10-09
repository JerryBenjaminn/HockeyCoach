namespace HockeyCoach.Harness.Data;

/// <summary>Thrown when a data file is invalid. The message lists every error with its JSON path.</summary>
public sealed class DataLoadException : Exception
{
    /// <summary>Creates the exception.</summary>
    public DataLoadException(string fileName, IReadOnlyList<string> errors)
        : base(fileName + " is invalid:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(e => "  " + fileName + ": " + e)))
    {
        FileName = fileName;
        Errors = errors;
    }

    /// <summary>Name of the invalid file.</summary>
    public string FileName { get; }

    /// <summary>Errors as "<c>path: message</c>".</summary>
    public IReadOnlyList<string> Errors { get; }
}

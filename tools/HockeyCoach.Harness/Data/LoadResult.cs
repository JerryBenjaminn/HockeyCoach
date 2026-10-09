namespace HockeyCoach.Harness.Data;

/// <summary>Outcome of loading one data file: the value when valid, otherwise every error found.</summary>
/// <typeparam name="T">Loaded object type.</typeparam>
public sealed class LoadResult<T>
    where T : class
{
    private LoadResult(T? value, IReadOnlyList<string> errors)
    {
        Value = value;
        Errors = errors;
    }

    /// <summary>The loaded object; null when <see cref="Errors"/> is not empty.</summary>
    public T? Value { get; }

    /// <summary>Errors as "<c>path: message</c>", in the order found.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Whether loading succeeded.</summary>
    public bool Success => Errors.Count == 0 && Value != null;

    /// <summary>A successful result.</summary>
    public static LoadResult<T> Ok(T value) => new(value, Array.Empty<string>());

    /// <summary>A failed result.</summary>
    public static LoadResult<T> Fail(IReadOnlyList<string> errors) => new(null, errors.Count > 0 ? errors : new[] { "(root): unknown error" });

    /// <summary>Returns the value or throws <see cref="DataLoadException"/> with all errors.</summary>
    public T GetOrThrow(string fileName)
    {
        if (!Success)
        {
            throw new DataLoadException(fileName, Errors);
        }

        return Value!;
    }
}

namespace BPM.Contracts;

/// <summary>
/// A piece of agent-facing text with per-language variants. The first variant
/// set becomes the <see cref="Default"/>, used as fallback when a requested
/// language is missing. A plain <see cref="string"/> converts implicitly to a
/// single-variant instance under <see cref="DefaultLanguage"/>, so existing
/// single-language specs and attribute values keep working unchanged.
/// </summary>
/// <remarks>
/// Language is chosen by the agent per call (it is the only party that sees the
/// language the user writes in), passed as the <c>language</c> tool argument and
/// resolved here. The transport does not decide language.
/// </remarks>
public sealed class LocalizedText
{
    /// <summary>Fallback language used for a plain string and when no match is found.</summary>
    public const string DefaultLanguage = "en";

    private readonly Dictionary<string, string> _byLanguage = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The fallback text: the first variant that was set.</summary>
    public string? Default { get; private set; }

    /// <summary>Adds or replaces the text for a language (ISO 639-1 code, e.g. "en", "ka").</summary>
    public LocalizedText Set(string language, string text)
    {
        _byLanguage[Normalize(language)] = text;
        Default ??= text;
        return this;
    }

    /// <summary>
    /// Resolves the best text for <paramref name="language"/>: exact (region-insensitive)
    /// match, otherwise <see cref="Default"/>. A null/blank language yields the default.
    /// </summary>
    public string? Resolve(string? language)
    {
        if (!string.IsNullOrWhiteSpace(language) &&
            _byLanguage.TryGetValue(Normalize(language), out var match))
            return match;
        return Default;
    }

    // "ka-GE" -> "ka"; trims region and casing so the agent can pass either form.
    private static string Normalize(string language)
    {
        var trimmed = language.Trim();
        var dash = trimmed.IndexOf('-');
        return (dash > 0 ? trimmed[..dash] : trimmed).ToLowerInvariant();
    }

    public static implicit operator LocalizedText(string text) =>
        new LocalizedText().Set(DefaultLanguage, text);
}

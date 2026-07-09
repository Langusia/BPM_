using BPM.Contracts;
using Xunit;

namespace BPM.Tests.Application;

public class LocalizedTextTests
{
    [Fact]
    public void Resolve_returns_exact_language_match()
    {
        var text = new LocalizedText().Set("en", "Hello").Set("ka", "გამარჯობა");

        Assert.Equal("Hello", text.Resolve("en"));
        Assert.Equal("გამარჯობა", text.Resolve("ka"));
    }

    [Fact]
    public void Resolve_falls_back_to_default_when_language_missing_or_null()
    {
        var text = new LocalizedText().Set("en", "Hello").Set("ka", "გამარჯობა");

        Assert.Equal("Hello", text.Resolve("fr")); // no French variant -> default
        Assert.Equal("Hello", text.Resolve(null));
        Assert.Equal("Hello", text.Resolve("   "));
    }

    [Fact]
    public void Default_is_the_first_variant_set()
    {
        var text = new LocalizedText().Set("ka", "გამარჯობა").Set("en", "Hello");

        Assert.Equal("გამარჯობა", text.Default);
        Assert.Equal("გამარჯობა", text.Resolve("de"));
    }

    [Fact]
    public void Implicit_string_becomes_the_default_english_variant()
    {
        LocalizedText text = "Plain";

        Assert.Equal("Plain", text.Default);
        Assert.Equal("Plain", text.Resolve("en"));
        Assert.Equal("Plain", text.Resolve("ka")); // falls back to default
    }

    [Fact]
    public void Resolve_ignores_region_and_casing()
    {
        var text = new LocalizedText().Set("en", "Hello").Set("ka", "გამარჯობა");

        Assert.Equal("გამარჯობა", text.Resolve("ka-GE"));
        Assert.Equal("გამარჯობა", text.Resolve("KA"));
    }
}

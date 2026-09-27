using Ember;
using Xunit;

namespace Ember.Core.Tests;

public class FireTests
{
    readonly Catalog _catalog = CatalogLoader.Load();

    [Fact]
    public void Demo_cards_compile_clean()
    {
        foreach (var bot in _catalog.Bots)
        {
            var compiled = Fire.Compile(bot, _catalog);
            var errors = compiled.Problems.Where(p => p.Severity == "error").ToList();
            Assert.True(errors.Count == 0, bot.Slug + ": " + string.Join("; ", errors.Select(e => e.Rule + " " + e.Message)));
            Assert.EndsWith("#demo", compiled.Situation.TrimEnd());
            Assert.Contains("System Rules {", compiled.Situation);
            Assert.DoesNotContain("%include", compiled.Situation);
            Assert.DoesNotContain("%include", compiled.World);
            Assert.DoesNotContain("[Mara]", compiled.Greeting);
            Assert.Contains("characterName", compiled.Json);
            Assert.Contains(bot.Title, compiled.Json);
        }
    }

    [Fact]
    public void Includes_expand_npc_prose()
    {
        var bot = _catalog.Bots.Single(b => b.Slug == "sunday-at-four");
        var compiled = Fire.Compile(bot, _catalog);
        Assert.Contains("six market sketches", compiled.Situation);
        Assert.Contains("Harbor Flat", compiled.World);
        Assert.Contains("![](SLOT-1)", compiled.Greeting);
        Assert.True(compiled.Points["title"] <= Fire.TitleLimit);
    }

    [Fact]
    public void Brackets_in_greeting_are_errors()
    {
        var bot = _catalog.Bots[0].Clone();
        bot.Greeting += "\n[Mara] waves.\n";
        var compiled = Fire.Compile(bot, _catalog);
        Assert.Contains(compiled.Problems, p => p.Rule == "BRACKETS" && p.Severity == "error");
    }

    [Fact]
    public void Missing_include_and_long_title_and_age()
    {
        var bot = _catalog.Bots[0].Clone();
        bot.Title = new string('A', 41);
        bot.Age = 17;
        bot.Situation = "%include npc:nobody#profile\n%rules\n%rules\n";
        var compiled = Fire.Compile(bot, _catalog);
        Assert.Contains(compiled.Problems, p => p.Rule == "LIMIT");
        Assert.Contains(compiled.Problems, p => p.Rule == "AGE");
        Assert.Contains(compiled.Problems, p => p.Rule == "MISSING");
        Assert.Contains(compiled.Problems, p => p.Rule == "RULES_DUP");
        Assert.Equal(1, Count(compiled.Situation, "System Rules {"));
    }

    [Fact]
    public void Skeleton_resolves_and_flags_todos()
    {
        var bot = Skeletons.Create(_catalog, "Landing Sunday", "shared-landing", new[] { "jonah", "mara" }, "harbor-flat");
        var compiled = Fire.Compile(bot, _catalog);
        Assert.DoesNotContain(compiled.Problems, p => p.Rule is "MISSING" or "MISSING_FRAGMENT" or "CYCLE");
        Assert.Contains(compiled.Problems, p => p.Rule == "TODO");
        Assert.Contains(compiled.Problems, p => p.Rule == "CHECKLIST");
        Assert.Contains("Jonah", compiled.Situation);
        Assert.DoesNotContain("[Jonah]", compiled.Greeting);
        Assert.Equal("landing-sunday", bot.Slug);
    }

    static int Count(string haystack, string needle)
    {
        var n = 0;
        var i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            n++;
            i += needle.Length;
        }
        return n;
    }
}

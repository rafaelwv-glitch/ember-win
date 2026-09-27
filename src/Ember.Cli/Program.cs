using Ember;

var catalog = CatalogLoader.Load();
var cmd = args.Length > 0 ? args[0] : "list";

switch (cmd)
{
    case "list":
        Console.WriteLine("engines");
        foreach (var engine in catalog.Engines)
            Console.WriteLine($"  {engine.Id,-18} {engine.Title} — {engine.Machine}");
        Console.WriteLine("people");
        foreach (var npc in catalog.Npcs)
            Console.WriteLine($"  {npc.Id,-18} {npc.Name}, {npc.Age}, {npc.Job}");
        Console.WriteLine("places");
        foreach (var place in catalog.Locations)
            Console.WriteLine($"  {place.Id,-18} {place.Title}");
        Console.WriteLine("cards");
        foreach (var bot in catalog.Bots)
            Console.WriteLine($"  {bot.Slug,-18} {bot.Title}");
        return 0;
    case "fire":
        var slug = args.Length > 1 ? args[1] : catalog.Bots[0].Slug;
        var found = catalog.Bots.FirstOrDefault(b => b.Slug == slug);
        if (found is null)
        {
            Console.Error.WriteLine($"No demo card \"{slug}\".");
            return 2;
        }
        var fired = Fire.Compile(found, catalog);
        Console.WriteLine(fired.Json);
        Console.WriteLine();
        if (fired.Problems.Count == 0) Console.WriteLine("checks: clean");
        foreach (var problem in fired.Problems)
            Console.WriteLine($"{problem.Severity}\t{problem.Rule}\t{problem.Section}\t{problem.Message}");
        return fired.Ok ? 0 : 1;
    case "check":
        return Check(catalog) ? 0 : 1;
    default:
        Console.Error.WriteLine("Usage: ember list | ember fire [slug] | ember check");
        return 2;
}

static bool Check(Catalog catalog)
{
    var ok = true;
    void Fail(string message)
    {
        Console.Error.WriteLine("FAIL " + message);
        ok = false;
    }

    foreach (var bot in catalog.Bots)
    {
        var compiled = Fire.Compile(bot, catalog);
        var errors = compiled.Problems.Where(p => p.Severity == "error").ToList();
        if (errors.Count != 0)
            Fail(bot.Slug + ": " + string.Join("; ", errors.Select(e => e.Rule + " " + e.Message)));
        if (!compiled.Situation.TrimEnd().EndsWith("#demo", StringComparison.Ordinal))
            Fail(bot.Slug + " situation signature");
        if (!compiled.Situation.Contains("System Rules {", StringComparison.Ordinal))
            Fail(bot.Slug + " missing rules");
        if (compiled.Situation.Contains("%include", StringComparison.Ordinal) || compiled.World.Contains("%include", StringComparison.Ordinal))
            Fail(bot.Slug + " unresolved include");
        if (compiled.Greeting.Contains("[Mara]", StringComparison.Ordinal))
            Fail(bot.Slug + " bracket in greeting");
        if (!compiled.Json.Contains("characterName", StringComparison.Ordinal) || !compiled.Json.Contains(bot.Title, StringComparison.Ordinal))
            Fail(bot.Slug + " json");
        if (compiled.Points["title"] > Fire.TitleLimit)
            Fail(bot.Slug + " title budget");
    }

    var sunday = catalog.Bots.Single(b => b.Slug == "sunday-at-four");
    var sundayCompiled = Fire.Compile(sunday, catalog);
    if (!sundayCompiled.Situation.Contains("six market sketches", StringComparison.Ordinal))
        Fail("sunday prose");
    if (!sundayCompiled.World.Contains("narrow flat", StringComparison.Ordinal))
        Fail("sunday place");
    if (!sundayCompiled.Greeting.Contains("![](SLOT-1)", StringComparison.Ordinal))
        Fail("sunday slot");

    var broken = sunday.Clone();
    broken.Title = new string('A', 41);
    broken.Age = 17;
    broken.Greeting += "\n[Mara] waves.\n";
    broken.Situation = "%include npc:nobody#profile\n%rules\n%rules\n";
    var brokenCompiled = Fire.Compile(broken, catalog);
    foreach (var rule in new[] { "LIMIT", "AGE", "BRACKETS", "MISSING", "RULES_DUP" })
    {
        if (!brokenCompiled.Problems.Any(p => p.Rule == rule))
            Fail("expected " + rule);
    }
    if (Count(brokenCompiled.Situation, "System Rules {") != 1)
        Fail("rules duplicated in output");

    var fresh = Skeletons.Create(catalog, "Landing Sunday", "shared-landing", new[] { "jonah", "mara" }, "harbor-flat");
    var freshCompiled = Fire.Compile(fresh, catalog);
    if (freshCompiled.Problems.Any(p => p.Rule is "MISSING" or "MISSING_FRAGMENT" or "CYCLE"))
        Fail("skeleton unresolved");
    if (!freshCompiled.Problems.Any(p => p.Rule == "TODO") || !freshCompiled.Problems.Any(p => p.Rule == "CHECKLIST"))
        Fail("skeleton warnings");
    if (!freshCompiled.Situation.Contains("Jonah", StringComparison.Ordinal))
        Fail("skeleton cast");
    if (freshCompiled.Greeting.Contains("[Jonah]", StringComparison.Ordinal))
        Fail("skeleton greeting brackets");
    if (fresh.Slug != "landing-sunday")
        Fail("slug");

    if (ok) Console.WriteLine("check: ok");
    return ok;
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

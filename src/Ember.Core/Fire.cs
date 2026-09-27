using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Ember;

public static class Fire
{
    public const int TitleLimit = 40;
    public const int BioLimit = 500;
    public const int FieldLimit = 10_000;
    public const int ReplyLimit = 5_000;

    static readonly Regex IncludeRe = new(@"^%include\s+(npc|location):([a-z0-9-]+)#([a-z]+)\s*$", RegexOptions.Compiled);
    static readonly Regex RulesRe = new(@"^%rules\s*$", RegexOptions.Compiled);
    static readonly Regex SignatureRe = new(@"^#[a-z0-9-]+$", RegexOptions.Compiled);
    static readonly Regex BracketRe = new(@"\[[A-Za-z][A-Za-z' -]{0,40}\]", RegexOptions.Compiled);
    static readonly Regex SlotRe = new(@"!\[\]\(SLOT-\d+\)", RegexOptions.Compiled);

    static readonly string[] WorldHeaders =
    [
        "Setting Overview",
        "Primary Zones",
        "Pressure",
        "Dynamic / Mechanic",
        "Atmosphere",
        "NPC Movement",
        "Final World Instruction"
    ];

    public static int CodePoints(string value) => value.EnumerateRunes().Count();

    public static Compiled Compile(Bot bot, Catalog catalog)
    {
        var problems = new List<Problem>();
        var title = Expand(bot.Title, "title", catalog, problems, applyPost: false);
        var bio = Expand(bot.Bio, "bio", catalog, problems, applyPost: false);
        var situation = Expand(bot.Situation, "situation", catalog, problems, applyPost: true);
        var world = Expand(bot.World, "world", catalog, problems, applyPost: false);
        var greeting = Expand(bot.Greeting, "greeting", catalog, problems, applyPost: false);
        var reply = Expand(bot.Reply, "reply", catalog, problems, applyPost: false);

        if (string.IsNullOrWhiteSpace(title.Text))
            problems.Add(new Problem("error", "TITLE", "Name is empty.", "title"));
        if (bot.Age < 18)
            problems.Add(new Problem("error", "AGE", "Age must be 18 or older.", "title"));

        var engine = catalog.Engine(bot.EngineId);
        if (engine is null)
            problems.Add(new Problem("error", "ENGINE", $"Unknown engine \"{bot.EngineId}\".", "title"));
        if (bot.Cast.Count == 0)
            problems.Add(new Problem("error", "CAST", "Pick at least one person.", "situation"));

        foreach (var id in bot.Cast)
        {
            var npc = catalog.Npc(id);
            if (npc is null)
                problems.Add(new Problem("error", "CAST", $"Unknown person \"{id}\".", "situation"));
            else if (npc.Age < 18)
                problems.Add(new Problem("error", "AGE", $"{npc.Name} must be 18 or older.", "situation"));
        }

        if (BracketRe.IsMatch(greeting.Text))
            problems.Add(new Problem("error", "BRACKETS", "Square-bracket names belong in Situation, not the greeting.", "greeting"));

        var headerIndexes = WorldHeaders.Select(h => world.Text.IndexOf(h, StringComparison.Ordinal)).ToList();
        for (var i = 0; i < WorldHeaders.Length; i++)
        {
            if (headerIndexes[i] < 0)
            {
                var severity = WorldHeaders[i] == "Final World Instruction" ? "error" : "warning";
                problems.Add(new Problem(severity, "WORLD", $"Missing world header \"{WorldHeaders[i]}\".", "world"));
            }
        }
        var lastHeader = headerIndexes.Select((index, i) => (index, i)).Where(x => x.index >= 0).OrderBy(x => x.index).LastOrDefault();
        if (lastHeader.index >= 0 && WorldHeaders[lastHeader.i] != "Final World Instruction")
            problems.Add(new Problem("warning", "WORLD", "Final World Instruction should be the last world header.", "world"));

        if (!SlotRe.IsMatch(greeting.Text))
            problems.Add(new Problem("warning", "SLOTS", "Greeting has no image slots. Add ![](SLOT-1) on its own line.", "greeting"));

        foreach (var (section, text) in new[] { ("title", title.Text), ("bio", bio.Text), ("situation", situation.Text), ("world", world.Text), ("greeting", greeting.Text), ("reply", reply.Text) })
        {
            if (text.Contains("TODO", StringComparison.Ordinal))
                problems.Add(new Problem("warning", "TODO", "Unresolved TODO.", section));
        }

        if (engine is not null)
        {
            var blob = $"{situation.Text}\n{world.Text}\n{greeting.Text}";
            foreach (var phrase in engine.Forbidden)
            {
                if (blob.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                    problems.Add(new Problem("warning", "FORBIDDEN", $"Soft line from the engine: \"{phrase}\".", "situation"));
            }
            foreach (var fact in engine.RequiredFacts)
            {
                if (!bot.Checklist.TryGetValue(fact, out var done) || !done)
                    problems.Add(new Problem("warning", "CHECKLIST", $"Required fact not ticked: {fact}", "situation"));
            }
        }

        var points = new Dictionary<string, int>
        {
            ["title"] = CodePoints(title.Text),
            ["bio"] = CodePoints(bio.Text),
            ["situation"] = CodePoints(situation.Text),
            ["world"] = CodePoints(world.Text),
            ["greeting"] = CodePoints(greeting.Text),
            ["reply"] = CodePoints(reply.Text)
        };
        CheckBudget(problems, "title", points["title"], TitleLimit);
        CheckBudget(problems, "bio", points["bio"], BioLimit);
        CheckBudget(problems, "situation", points["situation"], FieldLimit);
        CheckBudget(problems, "world", points["world"], FieldLimit);
        CheckBudget(problems, "greeting", points["greeting"], FieldLimit);
        CheckBudget(problems, "reply", points["reply"], ReplyLimit);

        var lead = bot.Cast.Count > 0 ? catalog.Npc(bot.Cast[0]) : null;
        var gender = lead?.Gender switch
        {
            "male" => 1,
            "nonbinary" => 2,
            _ => 0
        };

        var compiled = new Compiled
        {
            Title = title.Text,
            Bio = bio.Text,
            Situation = situation.Text,
            World = world.Text,
            Greeting = greeting.Text,
            Reply = reply.Text,
            Gender = gender,
            Points = points,
            Problems = problems.OrderBy(p => p.Severity == "error" ? 0 : 1).ThenBy(p => p.Rule).ToList(),
            Json = "",
            Markdown = ""
        };

        return new Compiled
        {
            Title = compiled.Title,
            Bio = compiled.Bio,
            Situation = compiled.Situation,
            World = compiled.World,
            Greeting = compiled.Greeting,
            Reply = compiled.Reply,
            Gender = compiled.Gender,
            Points = compiled.Points,
            Problems = compiled.Problems,
            Json = EmitJson(bot, compiled),
            Markdown = EmitMarkdown(bot, compiled)
        };
    }

    static void CheckBudget(List<Problem> problems, string section, int count, int limit)
    {
        if (count > limit)
            problems.Add(new Problem("error", "LIMIT", $"{Label(section)} is {count} / {limit} characters.", section));
        else if (count >= (int)(limit * 0.8))
            problems.Add(new Problem("warning", "BUDGET", $"{Label(section)} is {count} / {limit} characters.", section));
    }

    public static string Label(string section) => section switch
    {
        "title" => "Name",
        "bio" => "Introduction",
        "situation" => "Situation",
        "world" => "World",
        "greeting" => "Greeting",
        "reply" => "Reply style",
        _ => section
    };

    static string EmitJson(Bot bot, Compiled c)
    {
        var node = new JsonObject
        {
            ["characterName"] = c.Title,
            ["introduction"] = c.Bio,
            ["setting"] = c.Situation,
            ["scenario"] = c.World,
            ["greeting"] = c.Greeting,
            ["characterAge"] = bot.Age,
            ["gender"] = c.Gender,
            ["rating"] = 0,
            ["engineId"] = bot.EngineId,
            ["signature"] = "#demo"
        };
        return node.ToJsonString(CatalogLoader.JsonOptions());
    }

    static string EmitMarkdown(Bot bot, Compiled c)
    {
        var sb = new StringBuilder();
        sb.Append("# ").AppendLine(c.Title);
        sb.AppendLine();
        sb.Append("- engine: ").AppendLine(bot.EngineId);
        sb.AppendLine("- signature: #demo");
        sb.Append("- age: ").Append(bot.Age).AppendLine();
        sb.AppendLine();
        sb.AppendLine("## Title + Bio");
        sb.AppendLine();
        sb.AppendLine(c.Title);
        sb.AppendLine();
        sb.AppendLine(c.Bio);
        sb.AppendLine();
        sb.AppendLine("## Situation");
        sb.AppendLine();
        sb.AppendLine(c.Situation);
        sb.AppendLine();
        sb.AppendLine("## World");
        sb.AppendLine();
        sb.AppendLine(c.World);
        sb.AppendLine();
        sb.AppendLine("## Opening");
        sb.AppendLine();
        sb.AppendLine(c.Greeting);
        sb.AppendLine();
        sb.AppendLine("## Reply Style");
        sb.AppendLine();
        sb.AppendLine(c.Reply);
        return sb.ToString().TrimEnd() + "\n";
    }

    sealed class Expanded
    {
        public required string Text { get; init; }
    }

    static Expanded Expand(string source, string section, Catalog catalog, List<Problem> problems, bool applyPost)
    {
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var parts = new List<string>();
        var rulesCount = 0;
        foreach (var line in lines)
        {
            var include = IncludeRe.Match(line);
            if (include.Success)
            {
                parts.Add(Resolve(include.Groups[1].Value, include.Groups[2].Value, include.Groups[3].Value, section, catalog, problems, 0, new HashSet<string>()));
                continue;
            }
            if (RulesRe.IsMatch(line))
            {
                if (!applyPost)
                {
                    problems.Add(new Problem("warning", "RULES_SCOPE", "%rules only belongs in Situation.", section));
                    continue;
                }
                rulesCount++;
                if (rulesCount == 1) parts.Add(catalog.Rules);
                else problems.Add(new Problem("error", "RULES_DUP", "System Rules can appear only once.", "situation"));
                continue;
            }
            if (SignatureRe.IsMatch(line.Trim()))
            {
                if (line.Trim() != catalog.Signature)
                    problems.Add(new Problem("error", "SIG_FOREIGN", "Only the demo signature #demo is allowed.", section));
                continue;
            }
            parts.Add(line);
        }

        var text = string.Join("\n", parts);
        text = Regex.Replace(text, @"\n{3,}", "\n\n").TrimEnd();
        if (applyPost)
        {
            if (rulesCount == 0)
                text = text.Length == 0 ? catalog.Rules : text + "\n\n" + catalog.Rules;
            text = text + "\n" + catalog.Signature;
        }
        return new Expanded { Text = text };
    }

    static string Resolve(string kind, string id, string fragment, string section, Catalog catalog, List<Problem> problems, int depth, HashSet<string> stack)
    {
        var key = $"{kind}:{id}#{fragment}";
        if (depth > 8 || !stack.Add(key))
        {
            problems.Add(new Problem("error", "CYCLE", $"Include cycle at {key}.", section));
            return "";
        }
        string? body = null;
        if (kind == "npc")
        {
            var npc = catalog.Npc(id);
            if (npc is null)
            {
                problems.Add(new Problem("error", "MISSING", $"Unknown person \"{id}\".", section));
                return $"⟦missing npc:{id}#{fragment}⟧";
            }
            body = npc.Fragment(fragment);
        }
        else if (kind == "location")
        {
            var place = catalog.Location(id);
            if (place is null)
            {
                problems.Add(new Problem("error", "MISSING", $"Unknown place \"{id}\".", section));
                return $"⟦missing location:{id}#{fragment}⟧";
            }
            body = place.Fragment(fragment);
        }
        if (body is null)
        {
            problems.Add(new Problem("error", "MISSING_FRAGMENT", $"No fragment #{fragment} on {kind}:{id}.", section));
            return "";
        }
        return Expand(body, section, catalog, problems, applyPost: false).Text.Trim('\n');
    }
}

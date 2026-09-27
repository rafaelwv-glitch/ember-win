using System.Text;
using System.Text.RegularExpressions;

namespace Ember;

public static class Skeletons
{
    public static string Slugify(string title)
    {
        var slug = title.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-").Trim('-');
        return slug.Length == 0 ? "card" : slug;
    }

    public static Bot Create(Catalog catalog, string title, string engineId, IReadOnlyList<string> cast, string placeId)
    {
        if (cast.Count == 0) throw new ArgumentException("Pick at least one person.", nameof(cast));
        var engine = catalog.Engine(engineId) ?? throw new ArgumentException("Unknown engine.", nameof(engineId));
        var place = catalog.Location(placeId) ?? throw new ArgumentException("Unknown place.", nameof(placeId));
        var lead = catalog.Npc(cast[0]) ?? throw new ArgumentException("Unknown person.", nameof(cast));
        var first = lead.Name.Split(' ')[0];

        var situation = new StringBuilder();
        situation.Append("%include npc:").Append(lead.Id).AppendLine("#profile");
        situation.AppendLine();
        situation.AppendLine("Where they are right now:");
        situation.Append("%include npc:").Append(lead.Id).AppendLine("#where");
        situation.AppendLine();
        situation.AppendLine("Look:");
        situation.Append("%include npc:").Append(lead.Id).AppendLine("#look");
        situation.AppendLine();
        situation.AppendLine("Voice:");
        situation.Append("%include npc:").Append(lead.Id).AppendLine("#voice");
        foreach (var id in cast.Skip(1))
        {
            situation.AppendLine();
            situation.Append("%include npc:").Append(id).AppendLine("#profile");
        }
        situation.AppendLine();
        situation.AppendLine("%rules");

        var world =
            "Setting Overview\n" +
            $"%include location:{place.Id}#overview\n\n" +
            "Primary Zones\n" +
            $"%include location:{place.Id}#zones\n\n" +
            "Pressure\n" +
            engine.TimeModel + "\n" +
            "TODO — spell what changes if this slips.\n\n" +
            "Dynamic / Mechanic\n" +
            engine.Machine + "\n\n" +
            "Atmosphere\n" +
            $"%include location:{place.Id}#atmosphere\n\n" +
            "NPC Movement\n" +
            engine.Interrupt + "\n\n" +
            "Final World Instruction\n" +
            "If {{user}} waits, other people may still act. A promise, a clock, or a shared room does not pause itself.\n";

        var greeting =
            $"*{place.Title} is already in the middle of something.*\n\n" +
            "![](SLOT-1)\n\n" +
            $"**{first}:** \"You're here. The clock is not waiting.\"\n\n" +
            "![](SLOT-2)\n";

        var reply =
            "Answer as the person in the room.\n" +
            "Stay with what you can see.\n" +
            $"Do not speak for {first}.\n" +
            "If you wait, the scene can still move.\n";

        return new Bot
        {
            Slug = Slugify(title),
            Title = title.Trim(),
            Bio = engine.Machine,
            EngineId = engine.Id,
            Cast = cast.ToList(),
            Places = new List<string> { place.Id },
            Age = 25,
            Situation = situation.ToString(),
            World = world,
            Greeting = greeting,
            Reply = reply,
            Checklist = engine.RequiredFacts.ToDictionary(f => f, _ => false)
        };
    }
}

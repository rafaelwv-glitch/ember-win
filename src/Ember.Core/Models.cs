namespace Ember;

public sealed class Catalog
{
    public required string Signature { get; init; }
    public required string Rules { get; init; }
    public required IReadOnlyList<Npc> Npcs { get; init; }
    public required IReadOnlyList<Location> Locations { get; init; }
    public required IReadOnlyList<Engine> Engines { get; init; }
    public required IReadOnlyList<Bot> Bots { get; init; }

    public Npc? Npc(string id) => Npcs.FirstOrDefault(n => n.Id == id);
    public Location? Location(string id) => Locations.FirstOrDefault(n => n.Id == id);
    public Engine? Engine(string id) => Engines.FirstOrDefault(n => n.Id == id);
}

public sealed class Npc
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int Age { get; init; }
    public required string Gender { get; init; }
    public required string Role { get; init; }
    public required string Job { get; init; }
    public required string Summary { get; init; }
    public required string Profile { get; init; }
    public required string Where { get; init; }
    public required string Look { get; init; }
    public required string Voice { get; init; }

    public string? Fragment(string name) => name switch
    {
        "profile" => Profile,
        "where" => Where,
        "look" => Look,
        "voice" => Voice,
        _ => null
    };
}

public sealed class Location
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public required string Overview { get; init; }
    public required string Zones { get; init; }
    public required string Atmosphere { get; init; }

    public string? Fragment(string name) => name switch
    {
        "overview" => Overview,
        "zones" => Zones,
        "atmosphere" => Atmosphere,
        _ => null
    };
}

public sealed class Engine
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Pillar { get; init; }
    public required string Machine { get; init; }
    public required IReadOnlyList<string> Jobs { get; init; }
    public required IReadOnlyList<string> RequiredFacts { get; init; }
    public required IReadOnlyList<string> Forbidden { get; init; }
    public required string TimeModel { get; init; }
    public required string Interrupt { get; init; }
}

public sealed class Bot
{
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public required string Bio { get; set; }
    public required string EngineId { get; set; }
    public required List<string> Cast { get; set; }
    public required List<string> Places { get; set; }
    public required int Age { get; set; }
    public required string Situation { get; set; }
    public required string World { get; set; }
    public required string Greeting { get; set; }
    public required string Reply { get; set; }
    public required Dictionary<string, bool> Checklist { get; set; }

    public Bot Clone() => new()
    {
        Slug = Slug,
        Title = Title,
        Bio = Bio,
        EngineId = EngineId,
        Cast = new List<string>(Cast),
        Places = new List<string>(Places),
        Age = Age,
        Situation = Situation,
        World = World,
        Greeting = Greeting,
        Reply = Reply,
        Checklist = new Dictionary<string, bool>(Checklist)
    };
}

public sealed record Problem(string Severity, string Rule, string Message, string? Section);

public sealed class Compiled
{
    public required string Title { get; init; }
    public required string Bio { get; init; }
    public required string Situation { get; init; }
    public required string World { get; init; }
    public required string Greeting { get; init; }
    public required string Reply { get; init; }
    public required string Json { get; init; }
    public required string Markdown { get; init; }
    public required int Gender { get; init; }
    public required IReadOnlyDictionary<string, int> Points { get; init; }
    public required IReadOnlyList<Problem> Problems { get; init; }

    public bool Ok => Problems.All(p => p.Severity != "error");
}

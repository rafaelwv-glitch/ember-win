using System.Text.Json;

namespace Ember;

public static class CatalogLoader
{
    public static Catalog Load()
    {
        using var stream = typeof(CatalogLoader).Assembly.GetManifestResourceStream("Ember.catalog.json")
            ?? throw new InvalidOperationException("Embedded catalog is missing.");
        using var reader = new StreamReader(stream);
        var file = JsonSerializer.Deserialize<CatalogDto>(reader.ReadToEnd(), JsonOptions())
            ?? throw new InvalidOperationException("Catalog JSON is empty.");
        return file.ToCatalog();
    }

    public static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    sealed class CatalogDto
    {
        public string Signature { get; set; } = "#demo";
        public string Rules { get; set; } = "";
        public List<Npc> Npcs { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
        public List<Engine> Engines { get; set; } = new();
        public List<Bot> Bots { get; set; } = new();

        public Catalog ToCatalog() => new()
        {
            Signature = Signature,
            Rules = Rules.Trim(),
            Npcs = Npcs,
            Locations = Locations,
            Engines = Engines,
            Bots = Bots.Select(b => b.Clone()).ToList()
        };
    }
}

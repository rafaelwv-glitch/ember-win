using Ember;

namespace Ember.App;

public static class Session
{
    public static Catalog Catalog { get; } = CatalogLoader.Load();
    public static List<Bot> Bots { get; } = Catalog.Bots.Select(bot => bot.Clone()).ToList();
    public static string ActiveSlug { get; private set; } = Bots[0].Slug;
    public static event Action? Changed;

    public static Bot Active => Bots.First(bot => bot.Slug == ActiveSlug);

    public static void Touch() => Changed?.Invoke();

    public static void Select(string slug)
    {
        if (Bots.Any(bot => bot.Slug == slug)) ActiveSlug = slug;
        Touch();
    }

    public static void Add(Bot bot)
    {
        var slug = bot.Slug;
        var n = 2;
        while (Bots.Any(item => item.Slug == slug))
        {
            slug = $"{bot.Slug}-{n}";
            n++;
        }
        bot.Slug = slug;
        Bots.Add(bot);
        ActiveSlug = slug;
        Touch();
    }

    public static void RemoveActive()
    {
        if (Bots.Count < 2) return;
        Bots.RemoveAll(bot => bot.Slug == ActiveSlug);
        ActiveSlug = Bots[0].Slug;
        Touch();
    }

    public static void Reset()
    {
        Bots.Clear();
        Bots.AddRange(Catalog.Bots.Select(bot => bot.Clone()));
        ActiveSlug = Bots[0].Slug;
        Touch();
    }
}

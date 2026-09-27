using Ember;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ember.App.Pages;

public sealed partial class LibraryPage : Page
{
    string _kind = "engines";
    bool _loading;

    public LibraryPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Fill();
    }

    void Kind_Click(object sender, RoutedEventArgs e)
    {
        _kind = (sender as Button)?.Tag as string ?? "engines";
        Fill();
    }

    void Fill()
    {
        _loading = true;
        Items.Items.Clear();
        if (_kind == "npcs")
        {
            foreach (var npc in Session.Catalog.Npcs)
                Items.Items.Add(new ListViewItem { Content = npc.Name, Tag = npc.Id });
        }
        else if (_kind == "places")
        {
            foreach (var place in Session.Catalog.Locations)
                Items.Items.Add(new ListViewItem { Content = place.Title, Tag = place.Id });
        }
        else
        {
            foreach (var engine in Session.Catalog.Engines)
                Items.Items.Add(new ListViewItem { Content = engine.Title, Tag = engine.Id });
        }
        if (Items.Items.Count > 0) Items.SelectedIndex = 0;
        _loading = false;
        Show(SelectedId());
    }

    void Items_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        Show(SelectedId());
    }

    string? SelectedId() => (Items.SelectedItem as ListViewItem)?.Tag as string;

    void Show(string? id)
    {
        if (id is null)
        {
            Detail.Text = "";
            return;
        }
        if (_kind == "npcs")
        {
            var npc = Session.Catalog.Npc(id);
            if (npc is null) return;
            Detail.Text =
                $"{npc.Name}\n{npc.Age} · {npc.Job} · {npc.Role}\n\n{npc.Summary}\n\n#profile\n{npc.Profile}\n\n#where\n{npc.Where}\n\n#look\n{npc.Look}\n\n#voice\n{npc.Voice}";
            return;
        }
        if (_kind == "places")
        {
            var place = Session.Catalog.Location(id);
            if (place is null) return;
            Detail.Text = $"{place.Title}\n\n{place.Summary}\n\n#overview\n{place.Overview}\n\n#zones\n{place.Zones}\n\n#atmosphere\n{place.Atmosphere}";
            return;
        }
        var engine = Session.Catalog.Engine(id);
        if (engine is null) return;
        Detail.Text =
            $"{engine.Title}\n{engine.Pillar}\n\n{engine.Machine}\n\nJobs\n- " +
            string.Join("\n- ", engine.Jobs) +
            "\n\nRequired facts\n- " + string.Join("\n- ", engine.RequiredFacts) +
            "\n\nForbidden soft lines\n- " + string.Join("\n- ", engine.Forbidden) +
            $"\n\nTime\n{engine.TimeModel}\n\nIf you wait\n{engine.Interrupt}";
    }
}

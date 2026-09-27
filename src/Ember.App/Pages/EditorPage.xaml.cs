using Ember;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ember.App.Pages;

public sealed partial class EditorPage : Page
{
    static readonly string[] Sections = ["title", "bio", "situation", "world", "greeting", "reply"];
    string _section = "situation";
    bool _loading;

    public EditorPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
        Unloaded += (_, _) => Session.Changed -= OnChanged;
        Session.Changed += OnChanged;
    }

    void OnChanged()
    {
        if (XamlRoot is null) return;
        Render();
    }

    void Render()
    {
        _loading = true;
        SectionBar.Children.Clear();
        foreach (var section in Sections)
        {
            var button = new Button
            {
                Content = Fire.Label(section),
                Tag = section,
                Margin = new Thickness(0, 0, 4, 0)
            };
            if (section == _section)
                button.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["EmberBrush"];
            button.Click += (_, _) =>
            {
                _section = section;
                Render();
            };
            SectionBar.Children.Add(button);
        }
        Source.Text = Read(Session.Active, _section);
        AgeBox.Value = Session.Active.Age;
        var compiled = Fire.Compile(Session.Active, Session.Catalog);
        BudgetText.Text = string.Join("\n", Sections.Select(section =>
        {
            var limit = section switch
            {
                "title" => Fire.TitleLimit,
                "bio" => Fire.BioLimit,
                "reply" => Fire.ReplyLimit,
                _ => Fire.FieldLimit
            };
            return $"{Fire.Label(section)}  {compiled.Points[section]} / {limit}";
        }));
        Facts.Children.Clear();
        var engine = Session.Catalog.Engine(Session.Active.EngineId);
        if (engine is not null)
        {
            foreach (var fact in engine.RequiredFacts)
            {
                var box = new CheckBox
                {
                    Content = fact,
                    IsChecked = Session.Active.Checklist.TryGetValue(fact, out var done) && done
                };
                box.Checked += (_, _) => SetFact(fact, true);
                box.Unchecked += (_, _) => SetFact(fact, false);
                Facts.Children.Add(box);
            }
        }
        _loading = false;
    }

    void SetFact(string fact, bool value)
    {
        if (_loading) return;
        Session.Active.Checklist[fact] = value;
    }

    void Age_Changed(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_loading || double.IsNaN(args.NewValue)) return;
        Session.Active.Age = (int)args.NewValue;
    }

    void Source_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        Write(Session.Active, _section, Source.Text);
    }

    void Remove_Click(object sender, RoutedEventArgs e) => Session.RemoveActive();

    static string Read(Bot bot, string section) => section switch
    {
        "title" => bot.Title,
        "bio" => bot.Bio,
        "world" => bot.World,
        "greeting" => bot.Greeting,
        "reply" => bot.Reply,
        _ => bot.Situation
    };

    static void Write(Bot bot, string section, string value)
    {
        switch (section)
        {
            case "title": bot.Title = value; break;
            case "bio": bot.Bio = value; break;
            case "world": bot.World = value; break;
            case "greeting": bot.Greeting = value; break;
            case "reply": bot.Reply = value; break;
            default: bot.Situation = value; break;
        }
    }
}

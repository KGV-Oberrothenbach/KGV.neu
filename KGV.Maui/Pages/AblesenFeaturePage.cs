namespace KGV.Maui.Pages;

public class AblesenFeaturePage : ContentPage
{
    public AblesenFeaturePage(string title, string description, string hint)
    {
        Title = title;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = title, FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = description, LineBreakMode = LineBreakMode.WordWrap },
                    new Border
                    {
                        Padding = 12,
                        Stroke = Colors.LightGray,
                        Content = new Label
                        {
                            Text = hint,
                            LineBreakMode = LineBreakMode.WordWrap,
                            TextColor = Colors.Gray
                        }
                    }
                }
            }
        };
    }
}

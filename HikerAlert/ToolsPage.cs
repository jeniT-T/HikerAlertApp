

namespace HikerAlert;

public class ToolsPage : ContentPage
{
    public ToolsPage()
    {
       Title = "Tools";

        Content = new VerticalStackLayout
        {
            Spacing = 12,
            Padding = 30,
            Children =
            {
                new Label
                {
                    Text = "Tools",
                    FontSize = 32,
                    HorizontalOptions = LayoutOptions.Center
                }, 
                new Label
                {
                    Text = "Hike Guidelines, Safety Guidelines and Emergency Guidelines are viewed here ",
                    FontSize = 18,
                    HorizontalOptions = LayoutOptions.Center
                }
            }
        };
    }
}
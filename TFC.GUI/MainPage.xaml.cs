using TFC.GUI.PageModels;

namespace TFC.GUI;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainPageModel vm)
            await vm.InitializeAsync();
    }
}
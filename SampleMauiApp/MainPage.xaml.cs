namespace SampleMauiApp;

public partial class MainPage : ContentPage
{
    private readonly WeatherApiClient _weatherApiClient;

    public MainPage(WeatherApiClient weatherApiClient)
    {
        InitializeComponent();

        _weatherApiClient = weatherApiClient;
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
        var weather = await _weatherApiClient.GetWeatherAsync();

        CounterBtn.Text = $"{weather[0].Summary}";
    }
}

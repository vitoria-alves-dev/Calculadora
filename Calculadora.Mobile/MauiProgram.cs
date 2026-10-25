using Microsoft.Extensions.Logging;

namespace Calculadora.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<Calculadora.Services.ICalculadoraAntecipacao, Calculadora.Services.CalculadoraAntecipacao>();
		builder.Services.AddTransient<ViewModels.MainViewModel>();
		builder.Services.AddTransient<MainPage>();

		return builder.Build();
	}
}

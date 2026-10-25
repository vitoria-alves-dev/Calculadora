using Calculadora.Mobile.ViewModels;
using Calculadora.Services;

namespace Calculadora.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage() : this(new CalculadoraAntecipacao())
    {
    }

    public MainPage(ICalculadoraAntecipacao calculadora)
    {
        InitializeComponent();
        BindingContext = new MainViewModel(calculadora);
    }
}

using Calculadora.Services;
using Calculadora.UI;

ICalculadoraAntecipacao calculadora = new CalculadoraAntecipacao();
var menu = new ConsoleMenu(calculadora);
menu.Executar();

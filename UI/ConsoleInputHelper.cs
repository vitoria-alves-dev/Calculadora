using System;
using System.Globalization;

namespace Calculadora.UI;

/// <summary>
/// Métodos utilitários para leitura robusta de dados via console com suporte a valores padrão e validações.
/// </summary>
public static class ConsoleInputHelper
{
    private static readonly CultureInfo CulturaPtBr = new("pt-BR");

    /// <summary>
    /// Lê uma data no formato brasileiro com valor padrão sugerido.
    /// </summary>
    public static DateTime LerData(string prompt, DateTime valorPadrao)
    {
        while (true)
        {
            Console.Write($"{prompt} [{valorPadrao:dd/MM/yyyy}]: ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) return valorPadrao;

            if (DateTime.TryParseExact(input, ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime resultado))
            {
                return resultado;
            }

            ExibirErro("Formato de data inválido. Use dia/mês/ano (ex: 20/10/2026).");
        }
    }

    /// <summary>
    /// Lê um valor decimal financeiro em reais, aceitando vírgula ou ponto.
    /// </summary>
    public static decimal LerDecimal(string prompt, decimal valorPadrao)
    {
        while (true)
        {
            Console.Write($"{prompt} [{valorPadrao:N2}]: ");
            string? input = Console.ReadLine()?.Trim().Replace("R$", "").Trim();
            if (string.IsNullOrEmpty(input)) return valorPadrao;

            input = input.Replace(".", ",");
            if (decimal.TryParse(input, NumberStyles.Any, CulturaPtBr, out decimal resultado) && resultado > 0)
            {
                return resultado;
            }

            ExibirErro("Valor numérico inválido. Informe um valor maior que zero.");
        }
    }

    /// <summary>
    /// Lê uma taxa percentual aceitando formato decimal (0.0219) ou percentual (2.19%).
    /// </summary>
    public static decimal LerPercentual(string prompt, decimal percentualPadrao)
    {
        while (true)
        {
            Console.Write($"{prompt} [{percentualPadrao:F2}%]: ");
            string? input = Console.ReadLine()?.Trim().Replace("%", "").Trim();
            if (string.IsNullOrEmpty(input)) return percentualPadrao / 100m;

            input = input.Replace(".", ",");
            if (decimal.TryParse(input, NumberStyles.Any, CulturaPtBr, out decimal resultado) && resultado >= 0)
            {
                // Se digitou por exemplo 2,19 interpreta como 2,19% (0.0219). Se já digitou 0.0219 interpreta diretamente.
                return resultado >= 1.0m ? resultado / 100m : resultado;
            }

            ExibirErro("Taxa percentual inválida. Tente novamente (ex: 2,10 ou 2.19).");
        }
    }

    /// <summary>
    /// Lê um número inteiro entre um limite mínimo e máximo.
    /// </summary>
    public static int LerInteiro(string prompt, int valorPadrao, int min = 1, int max = 120)
    {
        while (true)
        {
            Console.Write($"{prompt} [{valorPadrao}]: ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) return valorPadrao;

            if (int.TryParse(input, out int resultado) && resultado >= min && resultado <= max)
            {
                return resultado;
            }

            ExibirErro($"Informe um número inteiro válido entre {min} e {max}.");
        }
    }

    private static void ExibirErro(string mensagem)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(mensagem);
        Console.ResetColor();
    }
}

using Calculadora.Models;

namespace Calculadora.Services;

/// <summary>
/// Contrato para o motor de cálculo de antecipação de recebíveis.
/// </summary>
public interface ICalculadoraAntecipacao
{
    /// <summary>
    /// Realiza a simulação padrão a partir do valor bruto da venda.
    /// </summary>
    SimulacaoResultado Calcular(SimulacaoParametros parametros);

    /// <summary>
    /// Realiza a simulação reversa a partir do valor líquido desejado.
    /// </summary>
    SimulacaoResultado CalcularReverso(SimulacaoReversaParametros parametros);
}

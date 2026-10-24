using System;

namespace Calculadora.Models;

/// <summary>
/// Parâmetros de entrada para simulação reversa (meta por valor líquido).
/// </summary>
public record SimulacaoReversaParametros(
    DateTime DataVenda,
    DateTime DataRecebimento,
    decimal TaxaAntecipacaoMensal,
    decimal TaxaMdr,
    decimal ValorLiquidoDesejado,
    int QuantidadeParcelas
);

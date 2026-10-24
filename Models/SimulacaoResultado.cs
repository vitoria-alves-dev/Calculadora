using System;
using System.Collections.Generic;

namespace Calculadora.Models;

/// <summary>
/// Resultado consolidado da simulação de antecipação.
/// </summary>
public record SimulacaoResultado
{
    public DateTime DataVenda { get; init; }
    public DateTime DataRecebimento { get; init; }
    public decimal TaxaAntecipacaoMensal { get; init; }
    public decimal TaxaMdr { get; init; }
    public decimal ValorBrutoTotal { get; init; }
    public int QuantidadeParcelas { get; init; }
    public decimal ValorPrimeiraParcela { get; init; }
    public decimal ValorDemaisParcelas { get; init; }
    public decimal TotalDescontoMdr { get; init; }
    public decimal TotalDescontoAntecipacao { get; init; }
    public decimal TotalDescontos { get; init; }
    public decimal ValorLiquidoTotal { get; init; }
    public decimal CustoEfetivoTotalPercentual { get; init; }
    public decimal PercentualLiquidoReceber { get; init; }
    public IReadOnlyList<ParcelaDetalhe> Parcelas { get; init; } = [];
}

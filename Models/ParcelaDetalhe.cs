using System;

namespace Calculadora.Models;

/// <summary>
/// Detalhes calculados de uma parcela individual no cronograma.
/// </summary>
public record ParcelaDetalhe
{
    public int NumeroParcela { get; init; }
    public DateTime DataVenda { get; init; }
    public DateTime VencimentoOriginal { get; init; }
    public DateTime DataRecebimento { get; init; }
    public int DiasAntecipados { get; init; }
    public decimal TaxaAntecipacaoMensal { get; init; }
    public decimal TaxaAntecipacaoProporcional { get; init; }
    public decimal ValorBruto { get; init; }
    public decimal TaxaMdr { get; init; }
    public decimal DescontoMdr { get; init; }
    public decimal ValorSemTaxa { get; init; }
    public decimal DescontoAntecipacao { get; init; }
    public decimal ValorLiquido { get; init; }
}

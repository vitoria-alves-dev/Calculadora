using System;

namespace Calculadora.Models;

/// <summary>
/// Parâmetros de entrada para simulação de antecipação direta.
/// </summary>
public record SimulacaoParametros(
    DateTime DataVenda,
    DateTime DataRecebimento,
    decimal TaxaAntecipacaoMensal, // ex: 0.021m para 2,10%
    decimal TaxaMdr,               // ex: 0.0219m para 2,19%
    decimal ValorTotalVenda,       // ex: 658.00m
    int QuantidadeParcelas         // ex: 2
);

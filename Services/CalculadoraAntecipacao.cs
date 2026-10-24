using System;
using System.Collections.Generic;
using System.Linq;
using Calculadora.Models;

namespace Calculadora.Services;

/// <summary>
/// Motor de cálculo que replica fielmente as regras e fórmulas financeiras da planilha Calculadora V0.xlsx.
/// </summary>
public class CalculadoraAntecipacao : ICalculadoraAntecipacao
{
    /// <inheritdoc />
    public SimulacaoResultado Calcular(SimulacaoParametros p)
    {
        if (p.QuantidadeParcelas <= 0)
            throw new ArgumentException("A quantidade de parcelas deve ser maior que zero.", nameof(p));

        // 1. Divisão e ajuste dos centavos das parcelas brutas
        // Demais parcelas: ROUNDDOWN(Total * ROUND(1 / Parcelas, 4), 2)
        decimal fatorInverso = Round(1.0m / p.QuantidadeParcelas, 4);
        decimal valorDemaisParcelas = RoundDown(p.ValorTotalVenda * fatorInverso, 2);

        // 1ª Parcela: ROUND(Total - (Parcelas - 1) * DemaisParcelas, 2)
        decimal valorPrimeiraParcela = Round(p.ValorTotalVenda - (p.QuantidadeParcelas - 1) * valorDemaisParcelas, 2);

        var parcelas = new List<ParcelaDetalhe>(p.QuantidadeParcelas);

        for (int i = 1; i <= p.QuantidadeParcelas; i++)
        {
            // Vencimento original: WORKDAY(DataVenda + (i * 30) - 1, 1)
            DateTime dataBase = p.DataVenda.AddDays((i * 30) - 1);
            DateTime vencimentoOriginal = AdicionarDiasUteis(dataBase, 1);

            int diasAntecipados = (vencimentoOriginal - p.DataRecebimento).Days;

            // Taxa Proporcional ao dia: ROUNDDOWN((TaxaMensal / 30) * DiasAntecipados, 4)
            decimal taxaAntecipacaoDia = RoundDown((p.TaxaAntecipacaoMensal / 30.0m) * diasAntecipados, 4);

            decimal valorBruto = (i == 1) ? valorPrimeiraParcela : valorDemaisParcelas;

            // Desconto MDR:
            // Parcela 1 absorve o ajuste: ROUND(Total * MDR, 2) - ((Parcelas - 1) * ROUNDDOWN(Demais * MDR, 2))
            // Demais parcelas: ROUNDDOWN(Bruto * MDR, 2)
            decimal descontoMdr;
            if (i == 1)
            {
                decimal totalMdrEsperado = Round(p.ValorTotalVenda * p.TaxaMdr, 2);
                decimal mdrDemaisParcela = RoundDown(valorDemaisParcelas * p.TaxaMdr, 2);
                descontoMdr = totalMdrEsperado - ((p.QuantidadeParcelas - 1) * mdrDemaisParcela);
            }
            else
            {
                descontoMdr = RoundDown(valorBruto * p.TaxaMdr, 2);
            }

            decimal valorSemTaxa = valorBruto - descontoMdr;

            // Desconto Antecipação: ROUND(TaxaProporcional * ValorSemTaxa, 2)
            decimal descontoAntecipacao = Round(taxaAntecipacaoDia * valorSemTaxa, 2);

            decimal valorLiquido = valorSemTaxa - descontoAntecipacao;

            parcelas.Add(new ParcelaDetalhe
            {
                NumeroParcela = i,
                DataVenda = p.DataVenda,
                VencimentoOriginal = vencimentoOriginal,
                DataRecebimento = p.DataRecebimento,
                DiasAntecipados = diasAntecipados,
                TaxaAntecipacaoMensal = p.TaxaAntecipacaoMensal,
                TaxaAntecipacaoProporcional = taxaAntecipacaoDia,
                ValorBruto = valorBruto,
                TaxaMdr = p.TaxaMdr,
                DescontoMdr = descontoMdr,
                ValorSemTaxa = valorSemTaxa,
                DescontoAntecipacao = descontoAntecipacao,
                ValorLiquido = valorLiquido
            });
        }

        decimal totalDescontoMdr = parcelas.Sum(x => x.DescontoMdr);
        decimal totalDescontoAntecipacao = parcelas.Sum(x => x.DescontoAntecipacao);
        decimal totalDescontos = totalDescontoMdr + totalDescontoAntecipacao;
        decimal valorLiquidoTotal = parcelas.Sum(x => x.ValorLiquido);

        decimal custoEfetivoTotal = p.ValorTotalVenda > 0
            ? Round((totalDescontos / p.ValorTotalVenda) * 100m, 2)
            : 0m;

        decimal percentualLiquido = p.ValorTotalVenda > 0
            ? Round((valorLiquidoTotal / p.ValorTotalVenda) * 100m, 2)
            : 0m;

        return new SimulacaoResultado
        {
            DataVenda = p.DataVenda,
            DataRecebimento = p.DataRecebimento,
            TaxaAntecipacaoMensal = p.TaxaAntecipacaoMensal,
            TaxaMdr = p.TaxaMdr,
            ValorBrutoTotal = p.ValorTotalVenda,
            QuantidadeParcelas = p.QuantidadeParcelas,
            ValorPrimeiraParcela = valorPrimeiraParcela,
            ValorDemaisParcelas = valorDemaisParcelas,
            TotalDescontoMdr = totalDescontoMdr,
            TotalDescontoAntecipacao = totalDescontoAntecipacao,
            TotalDescontos = totalDescontos,
            ValorLiquidoTotal = valorLiquidoTotal,
            CustoEfetivoTotalPercentual = custoEfetivoTotal,
            PercentualLiquidoReceber = percentualLiquido,
            Parcelas = parcelas
        };
    }

    /// <inheritdoc />
    public SimulacaoResultado CalcularReverso(SimulacaoReversaParametros p)
    {
        if (p.QuantidadeParcelas <= 0)
            throw new ArgumentException("A quantidade de parcelas deve ser maior que zero.", nameof(p));

        // 1. Calcula os fatores líquidos de cada parcela: (1 - MDR) * (1 - TaxaDia)
        decimal somaFatores = 0m;

        for (int i = 1; i <= p.QuantidadeParcelas; i++)
        {
            DateTime dataBase = p.DataVenda.AddDays((i * 30) - 1);
            DateTime vencimentoOriginal = AdicionarDiasUteis(dataBase, 1);
            int diasAntecipados = (vencimentoOriginal - p.DataRecebimento).Days;
            decimal taxaAntecipacaoDia = RoundDown((p.TaxaAntecipacaoMensal / 30.0m) * diasAntecipados, 4);

            decimal fatorLiquidoParcela = (1.0m - p.TaxaMdr) * (1.0m - taxaAntecipacaoDia);
            somaFatores += fatorLiquidoParcela;
        }

        // Fator líquido médio: SUM(fatorLiquido) / parcelas
        decimal fatorLiquidoMedio = somaFatores / p.QuantidadeParcelas;

        // Valor bruto calculado: ROUND(ValorLiquidoDesejado / FatorLiquidoMedio, 2)
        decimal valorBrutoCalculado = fatorLiquidoMedio > 0
            ? Round(p.ValorLiquidoDesejado / fatorLiquidoMedio, 2)
            : 0m;

        // Com o valor bruto determinado, executa a simulação completa para consolidar as parcelas e os centavos
        return Calcular(new SimulacaoParametros(
            DataVenda: p.DataVenda,
            DataRecebimento: p.DataRecebimento,
            TaxaAntecipacaoMensal: p.TaxaAntecipacaoMensal,
            TaxaMdr: p.TaxaMdr,
            ValorTotalVenda: valorBrutoCalculado,
            QuantidadeParcelas: p.QuantidadeParcelas
        ));
    }

    /// <summary>
    /// Replica a função WORKDAY(startDate, days) do Excel (dias úteis de segunda a sexta-feira).
    /// </summary>
    public static DateTime AdicionarDiasUteis(DateTime dataInicial, int diasUteis)
    {
        DateTime atual = dataInicial;
        int adicionados = 0;
        int direcao = diasUteis >= 0 ? 1 : -1;
        int meta = Math.Abs(diasUteis);

        while (adicionados < meta)
        {
            atual = atual.AddDays(direcao);
            if (atual.DayOfWeek != DayOfWeek.Saturday && atual.DayOfWeek != DayOfWeek.Sunday)
            {
                adicionados++;
            }
        }

        return atual;
    }

    /// <summary>
    /// Trunca casas decimais sem arredondamento para cima (equivalente ao ROUNDDOWN do Excel).
    /// </summary>
    public static decimal RoundDown(decimal valor, int casasDecimais)
    {
        decimal fator = (decimal)Math.Pow(10, casasDecimais);
        return Math.Truncate(valor * fator) / fator;
    }

    /// <summary>
    /// Arredondamento aritmético padrão (equivalente ao ROUND do Excel, com midpoint afastando de zero).
    /// </summary>
    public static decimal Round(decimal valor, int casasDecimais)
    {
        return Math.Round(valor, casasDecimais, MidpointRounding.AwayFromZero);
    }
}

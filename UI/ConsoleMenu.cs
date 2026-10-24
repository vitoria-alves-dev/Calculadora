using System;
using System.IO;
using ClosedXML.Excel;
using Calculadora.Models;
using Calculadora.Services;

namespace Calculadora.UI;

/// <summary>
/// Gerencia a interface de usuário no console, menus e exibição dos relatórios da simulação.
/// </summary>
public class ConsoleMenu
{
    private readonly ICalculadoraAntecipacao _calculadora;

    public ConsoleMenu(ICalculadoraAntecipacao calculadora)
    {
        _calculadora = calculadora ?? throw new ArgumentNullException(nameof(calculadora));
    }

    /// <summary>
    /// Inicia o loop de interação com o usuário no console.
    /// </summary>
    public void Executar()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (true)
        {
            LimparTela();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================================");
            Console.WriteLine("             SIMULADOR DE ANTECIPAÇÃO DE RECEBÍVEIS (.NET 9)");
            Console.WriteLine("===============================================================================");
            Console.ResetColor();
            Console.WriteLine("  Escolha uma opção para simular:");
            Console.WriteLine("    [1] Simulação Padrão (Calcular valor líquido a receber)");
            Console.WriteLine("    [2] Simulação Reversa (Calcular valor a cobrar a partir do líquido)");
            Console.WriteLine("    [3] Teste Rápido com os Valores da Planilha (Calculadora V0.xlsx)");
            Console.WriteLine("    [0] Sair");
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.Write("  Digite a opção desejada: ");

            string? opcao = Console.ReadLine()?.Trim();

            if (opcao == "0")
            {
                Console.WriteLine("\nEncerrando o simulador. Até logo!");
                break;
            }

            switch (opcao)
            {
                case "1":
                    ExecutarSimulacaoPadrao();
                    break;
                case "2":
                    ExecutarSimulacaoReversa();
                    break;
                case "3":
                    ExecutarSimulacaoPlanilha();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nOpção inválida! Pressione ENTER para tentar novamente...");
                    Console.ResetColor();
                    Console.ReadLine();
                    continue;
            }

            Console.WriteLine("\nPressione ENTER para voltar ao menu principal...");
            Console.ReadLine();
        }
    }

    private void ExecutarSimulacaoPadrao()
    {
        LimparTela();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=== [1] NOVA SIMULAÇÃO PADRÃO ===");
        Console.WriteLine("Informe os parâmetros abaixo (ou aperte ENTER para usar o valor sugerido entre colchetes):\n");
        Console.ResetColor();

        DateTime dataVenda = ConsoleInputHelper.LerData("1. Data da Venda", DateTime.Today);
        DateTime dataRecebimento = ConsoleInputHelper.LerData("2. Data do Recebimento/Antecipação", dataVenda.AddDays(1));
        decimal valorVenda = ConsoleInputHelper.LerDecimal("3. Valor Total da Venda (R$)", 1000.00m);
        int parcelas = ConsoleInputHelper.LerInteiro("4. Quantidade de Parcelas", 2, min: 1, max: 36);
        decimal taxaAntecipacao = ConsoleInputHelper.LerPercentual("5. Taxa de Antecipação ao mês (% a.m.)", 2.10m);
        decimal taxaMdr = ConsoleInputHelper.LerPercentual("6. Taxa MDR / Bandeira (%)", 2.19m);

        var parametros = new SimulacaoParametros(
            DataVenda: dataVenda,
            DataRecebimento: dataRecebimento,
            TaxaAntecipacaoMensal: taxaAntecipacao,
            TaxaMdr: taxaMdr,
            ValorTotalVenda: valorVenda,
            QuantidadeParcelas: parcelas
        );

        var resultado = _calculadora.Calcular(parametros);

        ExibirRelatorioSimulacao(resultado);
    }

    private void ExecutarSimulacaoReversa()
    {
        LimparTela();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=== [2] NOVA SIMULAÇÃO REVERSA ===");
        Console.WriteLine("Descubra quanto cobrar do cliente para receber o valor líquido exato desejado.\n");
        Console.ResetColor();

        DateTime dataVenda = ConsoleInputHelper.LerData("1. Data da Venda", DateTime.Today);
        DateTime dataRecebimento = ConsoleInputHelper.LerData("2. Data do Recebimento/Antecipação", dataVenda.AddDays(1));
        decimal valorLiquidoDesejado = ConsoleInputHelper.LerDecimal("3. Valor Líquido que você deseja receber (R$)", 1000.00m);
        int parcelas = ConsoleInputHelper.LerInteiro("4. Quantidade de Parcelas", 2, min: 1, max: 36);
        decimal taxaAntecipacao = ConsoleInputHelper.LerPercentual("5. Taxa de Antecipação ao mês (% a.m.)", 2.10m);
        decimal taxaMdr = ConsoleInputHelper.LerPercentual("6. Taxa MDR / Bandeira (%)", 2.19m);

        var parametros = new SimulacaoReversaParametros(
            DataVenda: dataVenda,
            DataRecebimento: dataRecebimento,
            TaxaAntecipacaoMensal: taxaAntecipacao,
            TaxaMdr: taxaMdr,
            ValorLiquidoDesejado: valorLiquidoDesejado,
            QuantidadeParcelas: parcelas
        );

        var resultado = _calculadora.CalcularReverso(parametros);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n>> Para receber R$ {valorLiquidoDesejado:N2} líquido, você deve cobrar: R$ {resultado.ValorBrutoTotal:N2} <<\n");
        Console.ResetColor();

        ExibirRelatorioSimulacao(resultado);
    }

    private void ExecutarSimulacaoPlanilha()
    {
        LimparTela();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=== [3] TESTE COM DADOS DA PLANILHA Calculadora V0.xlsx ===");
        Console.ResetColor();

        string filePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Calculadora V0.xlsx");
        if (!File.Exists(filePath)) filePath = "Calculadora V0.xlsx";

        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Arquivo '{filePath}' não encontrado!");
            Console.ResetColor();
            return;
        }

        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet("Simulador");

        DateTime dtVenda = ws.Cell("C6").GetDateTime();
        DateTime dtRec = ws.Cell("C7").GetDateTime();
        decimal txAnt = (decimal)ws.Cell("C8").GetDouble();
        decimal txMdr = (decimal)ws.Cell("C9").GetDouble();
        decimal vlTotal = (decimal)ws.Cell("C10").GetDouble();
        int parcelas = (int)ws.Cell("C11").GetDouble();

        var parametros = new SimulacaoParametros(dtVenda, dtRec, txAnt, txMdr, vlTotal, parcelas);
        var resultado = _calculadora.Calcular(parametros);

        decimal excelBruto = (decimal)ws.Cell("G33").GetDouble();
        decimal excelMdr = (decimal)ws.Cell("H33").GetDouble();
        decimal excelAnt = (decimal)ws.Cell("I33").GetDouble();
        decimal excelLiq = (decimal)ws.Cell("J33").GetDouble();

        ExibirRelatorioSimulacao(resultado);

        Console.WriteLine("\n[VALIDAÇÃO COM AS CÉLULAS DA PLANILHA EXCEL]:");
        Console.WriteLine($"  - Bruto Total:    C# R$ {resultado.ValorBrutoTotal:N2} | Excel R$ {excelBruto:N2} => {(resultado.ValorBrutoTotal == excelBruto ? "100% IDÊNTICO ✓" : "DIVERGENTE ✗")}");
        Console.WriteLine($"  - Desconto MDR:   C# R$ {resultado.TotalDescontoMdr:N2} | Excel R$ {excelMdr:N2} => {(resultado.TotalDescontoMdr == excelMdr ? "100% IDÊNTICO ✓" : "DIVERGENTE ✗")}");
        Console.WriteLine($"  - Desconto Antec: C# R$ {resultado.TotalDescontoAntecipacao:N2} | Excel R$ {excelAnt:N2} => {(resultado.TotalDescontoAntecipacao == excelAnt ? "100% IDÊNTICO ✓" : "DIVERGENTE ✗")}");
        Console.WriteLine($"  - Valor Líquido:  C# R$ {resultado.ValorLiquidoTotal:N2} | Excel R$ {excelLiq:N2} => {(resultado.ValorLiquidoTotal == excelLiq ? "100% IDÊNTICO ✓" : "DIVERGENTE ✗")}");
    }

    private static void ExibirRelatorioSimulacao(SimulacaoResultado r)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("-------------------------------------------------------------------------------");
        Console.WriteLine("                             RESUMO DA OPERAÇÃO");
        Console.WriteLine("-------------------------------------------------------------------------------");
        Console.ResetColor();

        Console.WriteLine($"  Data da Venda:            {r.DataVenda:dd/MM/yyyy}");
        Console.WriteLine($"  Data do Recebimento:      {r.DataRecebimento:dd/MM/yyyy}");
        Console.WriteLine($"  Taxa MDR:                 {r.TaxaMdr:P2}");
        Console.WriteLine($"  Taxa Antecipação:         {r.TaxaAntecipacaoMensal:P2} a.m.");
        Console.WriteLine($"  Quantidade de Parcelas:   {r.QuantidadeParcelas}x");
        if (r.QuantidadeParcelas > 1)
        {
            Console.WriteLine($"  Divisão das Parcelas:     1ª de R$ {r.ValorPrimeiraParcela:N2} | Demais {r.QuantidadeParcelas - 1}x de R$ {r.ValorDemaisParcelas:N2}");
        }

        Console.WriteLine("-------------------------------------------------------------------------------");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  (+) VALOR BRUTO TOTAL:           R$ {r.ValorBrutoTotal,10:N2}");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  (-) Total Desconto MDR:          R$ {r.TotalDescontoMdr,10:N2}");
        Console.WriteLine($"  (-) Total Desconto Antecipação:  R$ {r.TotalDescontoAntecipacao,10:N2}");
        Console.WriteLine($"  (=) TOTAL GERAL DE DESCONTOS:    R$ {r.TotalDescontos,10:N2}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  (=) VALOR LÍQUIDO A RECEBER:     R$ {r.ValorLiquidoTotal,10:N2}");
        Console.ResetColor();
        Console.WriteLine($"  Custo Efetivo Total (CET):          {r.CustoEfetivoTotalPercentual,9:F2}%");
        Console.WriteLine($"  Percentual Líquido Recebido:        {r.PercentualLiquidoReceber,9:F2}%");

        Console.WriteLine("\n-------------------------------------------------------------------------------");
        Console.WriteLine("                        CRONOGRAMA DETALHADO POR PARCELA");
        Console.WriteLine("-------------------------------------------------------------------------------");
        Console.WriteLine("Parc | Vencimento | D.Antec | Tx.Dia  | Vl.Bruto  | Desc.MDR | Desc.Ant | Vl.Líquido");
        Console.WriteLine("-----+------------+---------+---------+-----------+----------+----------+-----------");

        foreach (var p in r.Parcelas)
        {
            Console.WriteLine($"{p.NumeroParcela,4} | {p.VencimentoOriginal:dd/MM/yyyy} | {p.DiasAntecipados,7} | {p.TaxaAntecipacaoProporcional,7:F4} | R${p.ValorBruto,7:N2} | R${p.DescontoMdr,6:N2} | R${p.DescontoAntecipacao,6:N2} | R${p.ValorLiquido,7:N2}");
        }
        Console.WriteLine("-------------------------------------------------------------------------------");
    }

    private static void LimparTela()
    {
        try
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }
        }
        catch
        {
            // Ignora exceção quando executado sem console interativo / em pipes
        }
    }
}

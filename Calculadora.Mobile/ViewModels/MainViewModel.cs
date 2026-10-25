using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Calculadora.Models;
using Calculadora.Services;

namespace Calculadora.Mobile.ViewModels;

public record BandeiraOpcao(string Nome, string Icone, decimal TaxaMdrSugerida)
{
    public override string ToString() => $"{Icone} {Nome}";
}

public record ParcelaDetalheItem(
    int NumeroParcela,
    string TituloParcela,
    string VencimentoTexto,
    string DiasAntecipadosTexto,
    string TaxaDiaTexto,
    string ValorBrutoTexto,
    string DescontoMdrTexto,
    string DescontoAntecipacaoTexto,
    string ValorLiquidoTexto
);

public class MainViewModel : BaseViewModel
{
    private readonly ICalculadoraAntecipacao _calculadora;
    private static readonly CultureInfo CulturaPtBr = new("pt-BR");

    private bool _isModoPadrao = true;
    private bool _isModoReverso = false;
    private string _valorBrutoTexto = "1000,00";
    private string _valorLiquidoTexto = "1000,00";
    private int _quantidadeParcelas = 2;
    private DateTime _dataVenda = DateTime.Today;
    private DateTime _dataRecebimento = DateTime.Today.AddDays(1);
    private BandeiraOpcao _bandeiraSelecionada;
    private string _taxaMdrTexto = "2,19";
    private string _taxaAntecipacaoTexto = "2,10";

    // Resultados
    private bool _temResultado = false;
    private bool _temMensagemErro = false;
    private string _mensagemErro = string.Empty;

    private string _tituloDestaquePrincipal = "VALOR LÍQUIDO FINAL A RECEBER";
    private string _subtituloDestaquePrincipal = "Valor creditado após descontos de MDR e antecipação";
    private string _valorDestaquePrincipalFormatado = "R$ 0,00";
    private string _descontoMdrFormatado = "R$ 0,00";
    private string _descontoAntecipacaoFormatado = "R$ 0,00";
    private string _totalDescontosFormatado = "R$ 0,00";
    private string _cetFormatado = "0,00%";
    private string _percentualLiquidoFormatado = "0,00%";
    private string _valorBrutoTotalFormatado = "R$ 0,00";
    private string _resumoParcelasFormatado = string.Empty;

    public MainViewModel(ICalculadoraAntecipacao calculadora)
    {
        _calculadora = calculadora ?? throw new ArgumentNullException(nameof(calculadora));

        Bandeiras = new ObservableCollection<BandeiraOpcao>
        {
            new("Mastercard", "💳", 2.19m),
            new("Visa", "💳", 2.19m),
            new("Elo", "💳", 2.49m),
            new("Hipercard", "💳", 2.39m),
            new("American Express", "💳", 2.89m),
            new("Pix / Débito", "⚡", 0.99m),
            new("Outra (Personalizada)", "⚙️", 2.00m)
        };

        _bandeiraSelecionada = Bandeiras[0];

        Parcelas = new ObservableCollection<ParcelaDetalheItem>();

        // Comandos
        CalcularCommand = new Command(ExecutarCalculo);
        AlternarModoPadraoCommand = new Command(() => AtivarModo(true));
        AlternarModoReversoCommand = new Command(() => AtivarModo(false));
        SelecionarParcelaRapidaCommand = new Command<string>(param =>
        {
            if (int.TryParse(param, out int p) && p >= 1 && p <= 36)
            {
                QuantidadeParcelas = p;
            }
        });
        IncrementarParcelasCommand = new Command(() =>
        {
            if (QuantidadeParcelas < 36) QuantidadeParcelas++;
        });
        DecrementarParcelasCommand = new Command(() =>
        {
            if (QuantidadeParcelas > 1) QuantidadeParcelas--;
        });
        DefinirRecebimentoHojeCommand = new Command(() => DataRecebimento = DataVenda);
        DefinirRecebimentoD1Command = new Command(() => DataRecebimento = DataVenda.AddDays(1));
        DefinirRecebimentoD30Command = new Command(() => DataRecebimento = DataVenda.AddDays(30));
        PreencherExemploPlanilhaCommand = new Command(PreencherComValoresDaPlanilha);
        LimparCommand = new Command(LimparCampos);

        // Executa uma simulação inicial padrão para o usuário já ver os números vivos na tela
        ExecutarCalculo();
    }

    public ObservableCollection<BandeiraOpcao> Bandeiras { get; }
    public ObservableCollection<ParcelaDetalheItem> Parcelas { get; }

    public ICommand CalcularCommand { get; }
    public ICommand AlternarModoPadraoCommand { get; }
    public ICommand AlternarModoReversoCommand { get; }
    public ICommand SelecionarParcelaRapidaCommand { get; }
    public ICommand IncrementarParcelasCommand { get; }
    public ICommand DecrementarParcelasCommand { get; }
    public ICommand DefinirRecebimentoHojeCommand { get; }
    public ICommand DefinirRecebimentoD1Command { get; }
    public ICommand DefinirRecebimentoD30Command { get; }
    public ICommand PreencherExemploPlanilhaCommand { get; }
    public ICommand LimparCommand { get; }

    public bool IsModoPadrao
    {
        get => _isModoPadrao;
        set
        {
            if (SetProperty(ref _isModoPadrao, value))
            {
                OnPropertyChanged(nameof(IsModoReverso));
                AtualizarTitulosDestaque();
            }
        }
    }

    public bool IsModoReverso
    {
        get => _isModoReverso;
        set
        {
            if (SetProperty(ref _isModoReverso, value))
            {
                OnPropertyChanged(nameof(IsModoPadrao));
                AtualizarTitulosDestaque();
            }
        }
    }

    public string ValorBrutoTexto
    {
        get => _valorBrutoTexto;
        set => SetProperty(ref _valorBrutoTexto, value);
    }

    public string ValorLiquidoTexto
    {
        get => _valorLiquidoTexto;
        set => SetProperty(ref _valorLiquidoTexto, value);
    }

    public int QuantidadeParcelas
    {
        get => _quantidadeParcelas;
        set
        {
            if (value >= 1 && value <= 36 && SetProperty(ref _quantidadeParcelas, value))
            {
                OnPropertyChanged(nameof(DescricaoParcelas));
            }
        }
    }

    public string DescricaoParcelas => $"{QuantidadeParcelas}x";

    public DateTime DataVenda
    {
        get => _dataVenda;
        set
        {
            if (SetProperty(ref _dataVenda, value))
            {
                if (_dataRecebimento < _dataVenda)
                {
                    DataRecebimento = _dataVenda.AddDays(1);
                }
            }
        }
    }

    public DateTime DataRecebimento
    {
        get => _dataRecebimento;
        set => SetProperty(ref _dataRecebimento, value);
    }

    public BandeiraOpcao BandeiraSelecionada
    {
        get => _bandeiraSelecionada;
        set
        {
            if (SetProperty(ref _bandeiraSelecionada, value) && value != null)
            {
                TaxaMdrTexto = value.TaxaMdrSugerida.ToString("F2", CulturaPtBr);
            }
        }
    }

    public string TaxaMdrTexto
    {
        get => _taxaMdrTexto;
        set => SetProperty(ref _taxaMdrTexto, value);
    }

    public string TaxaAntecipacaoTexto
    {
        get => _taxaAntecipacaoTexto;
        set => SetProperty(ref _taxaAntecipacaoTexto, value);
    }

    public bool TemResultado
    {
        get => _temResultado;
        set => SetProperty(ref _temResultado, value);
    }

    public bool TemMensagemErro
    {
        get => _temMensagemErro;
        set => SetProperty(ref _temMensagemErro, value);
    }

    public string MensagemErro
    {
        get => _mensagemErro;
        set
        {
            if (SetProperty(ref _mensagemErro, value))
            {
                TemMensagemErro = !string.IsNullOrWhiteSpace(value);
            }
        }
    }

    public string TituloDestaquePrincipal
    {
        get => _tituloDestaquePrincipal;
        private set => SetProperty(ref _tituloDestaquePrincipal, value);
    }

    public string SubtituloDestaquePrincipal
    {
        get => _subtituloDestaquePrincipal;
        private set => SetProperty(ref _subtituloDestaquePrincipal, value);
    }

    public string ValorDestaquePrincipalFormatado
    {
        get => _valorDestaquePrincipalFormatado;
        private set => SetProperty(ref _valorDestaquePrincipalFormatado, value);
    }

    public string DescontoMdrFormatado
    {
        get => _descontoMdrFormatado;
        private set => SetProperty(ref _descontoMdrFormatado, value);
    }

    public string DescontoAntecipacaoFormatado
    {
        get => _descontoAntecipacaoFormatado;
        private set => SetProperty(ref _descontoAntecipacaoFormatado, value);
    }

    public string TotalDescontosFormatado
    {
        get => _totalDescontosFormatado;
        private set => SetProperty(ref _totalDescontosFormatado, value);
    }

    public string CetFormatado
    {
        get => _cetFormatado;
        private set => SetProperty(ref _cetFormatado, value);
    }

    public string PercentualLiquidoFormatado
    {
        get => _percentualLiquidoFormatado;
        private set => SetProperty(ref _percentualLiquidoFormatado, value);
    }

    public string ValorBrutoTotalFormatado
    {
        get => _valorBrutoTotalFormatado;
        private set => SetProperty(ref _valorBrutoTotalFormatado, value);
    }

    public string ResumoParcelasFormatado
    {
        get => _resumoParcelasFormatado;
        private set => SetProperty(ref _resumoParcelasFormatado, value);
    }

    private void AtivarModo(bool padrao)
    {
        IsModoPadrao = padrao;
        IsModoReverso = !padrao;
        ExecutarCalculo();
    }

    private void AtualizarTitulosDestaque()
    {
        if (IsModoPadrao)
        {
            TituloDestaquePrincipal = "VALOR LÍQUIDO FINAL A RECEBER";
            SubtituloDestaquePrincipal = "Valor creditado após descontos de MDR e antecipação";
        }
        else
        {
            TituloDestaquePrincipal = "VALOR TOTAL A COBRAR (BRUTO)";
            SubtituloDestaquePrincipal = "Passe este valor na maquininha para receber a sua meta líquida";
        }
    }

    public void ExecutarCalculo()
    {
        MensagemErro = string.Empty;

        // 1. Validações das taxas
        if (!TentarConverterPercentual(TaxaMdrTexto, out decimal taxaMdr))
        {
            MensagemErro = "Informe uma Taxa de MDR válida (ex: 2,19).";
            TemResultado = false;
            return;
        }

        if (!TentarConverterPercentual(TaxaAntecipacaoTexto, out decimal taxaAntecipacao))
        {
            MensagemErro = "Informe uma Taxa de Antecipação mensal válida (ex: 2,10).";
            TemResultado = false;
            return;
        }

        if (QuantidadeParcelas <= 0)
        {
            MensagemErro = "A quantidade de parcelas deve ser de no mínimo 1.";
            TemResultado = false;
            return;
        }

        if (DataRecebimento < DataVenda)
        {
            MensagemErro = "A data de recebimento não pode ser anterior à data da venda.";
            TemResultado = false;
            return;
        }

        try
        {
            SimulacaoResultado resultado;

            if (IsModoPadrao)
            {
                if (!TentarConverterDecimal(ValorBrutoTexto, out decimal valorBruto) || valorBruto <= 0)
                {
                    MensagemErro = "Informe um Valor Bruto da venda maior que zero (ex: 1000,00).";
                    TemResultado = false;
                    return;
                }

                var param = new SimulacaoParametros(
                    DataVenda: DataVenda,
                    DataRecebimento: DataRecebimento,
                    TaxaAntecipacaoMensal: taxaAntecipacao,
                    TaxaMdr: taxaMdr,
                    ValorTotalVenda: valorBruto,
                    QuantidadeParcelas: QuantidadeParcelas
                );

                resultado = _calculadora.Calcular(param);
                ValorDestaquePrincipalFormatado = resultado.ValorLiquidoTotal.ToString("C2", CulturaPtBr);
            }
            else
            {
                if (!TentarConverterDecimal(ValorLiquidoTexto, out decimal valorLiquidoDesejado) || valorLiquidoDesejado <= 0)
                {
                    MensagemErro = "Informe um Valor Líquido desejado maior que zero (ex: 1000,00).";
                    TemResultado = false;
                    return;
                }

                var paramReverso = new SimulacaoReversaParametros(
                    DataVenda: DataVenda,
                    DataRecebimento: DataRecebimento,
                    TaxaAntecipacaoMensal: taxaAntecipacao,
                    TaxaMdr: taxaMdr,
                    ValorLiquidoDesejado: valorLiquidoDesejado,
                    QuantidadeParcelas: QuantidadeParcelas
                );

                resultado = _calculadora.CalcularReverso(paramReverso);
                ValorDestaquePrincipalFormatado = resultado.ValorBrutoTotal.ToString("C2", CulturaPtBr);
            }

            // Atualiza campos de destaque
            DescontoMdrFormatado = resultado.TotalDescontoMdr.ToString("C2", CulturaPtBr);
            DescontoAntecipacaoFormatado = resultado.TotalDescontoAntecipacao.ToString("C2", CulturaPtBr);
            TotalDescontosFormatado = resultado.TotalDescontos.ToString("C2", CulturaPtBr);
            CetFormatado = $"{resultado.CustoEfetivoTotalPercentual.ToString("F2", CulturaPtBr)}%";
            PercentualLiquidoFormatado = $"{resultado.PercentualLiquidoReceber.ToString("F2", CulturaPtBr)}%";
            ValorBrutoTotalFormatado = resultado.ValorBrutoTotal.ToString("C2", CulturaPtBr);

            if (resultado.QuantidadeParcelas > 1)
            {
                ResumoParcelasFormatado = $"1ª parcela de {resultado.ValorPrimeiraParcela.ToString("C2", CulturaPtBr)} + {resultado.QuantidadeParcelas - 1}x de {resultado.ValorDemaisParcelas.ToString("C2", CulturaPtBr)}";
            }
            else
            {
                ResumoParcelasFormatado = $"Parcela única de {resultado.ValorPrimeiraParcela.ToString("C2", CulturaPtBr)}";
            }

            // Atualiza lista detalhada de parcelas
            Parcelas.Clear();
            foreach (var p in resultado.Parcelas)
            {
                Parcelas.Add(new ParcelaDetalheItem(
                    NumeroParcela: p.NumeroParcela,
                    TituloParcela: $"Parcela {p.NumeroParcela} de {resultado.QuantidadeParcelas}",
                    VencimentoTexto: $"Vencimento: {p.VencimentoOriginal:dd/MM/yyyy}",
                    DiasAntecipadosTexto: $"{p.DiasAntecipados} dias antecipados",
                    TaxaDiaTexto: $"Tx. Dia: {(p.TaxaAntecipacaoProporcional * 100m).ToString("F4", CulturaPtBr)}%",
                    ValorBrutoTexto: p.ValorBruto.ToString("C2", CulturaPtBr),
                    DescontoMdrTexto: $"-{p.DescontoMdr.ToString("C2", CulturaPtBr)}",
                    DescontoAntecipacaoTexto: $"-{p.DescontoAntecipacao.ToString("C2", CulturaPtBr)}",
                    ValorLiquidoTexto: p.ValorLiquido.ToString("C2", CulturaPtBr)
                ));
            }

            AtualizarTitulosDestaque();
            TemResultado = true;
        }
        catch (Exception ex)
        {
            MensagemErro = $"Erro no cálculo: {ex.Message}";
            TemResultado = false;
        }
    }

    private void PreencherComValoresDaPlanilha()
    {
        // Valores fieis da planilha 'Calculadora V0.xlsx'
        DataVenda = new DateTime(2026, 10, 1);
        DataRecebimento = new DateTime(2026, 10, 2);
        TaxaAntecipacaoTexto = "2,10";
        TaxaMdrTexto = "2,19";
        ValorBrutoTexto = "658,00";
        ValorLiquidoTexto = "612,90";
        QuantidadeParcelas = 2;
        BandeiraSelecionada = Bandeiras[0]; // Mastercard
        IsModoPadrao = true;
        ExecutarCalculo();
    }

    private void LimparCampos()
    {
        ValorBrutoTexto = "1000,00";
        ValorLiquidoTexto = "1000,00";
        QuantidadeParcelas = 2;
        DataVenda = DateTime.Today;
        DataRecebimento = DateTime.Today.AddDays(1);
        BandeiraSelecionada = Bandeiras[0];
        TaxaMdrTexto = "2,19";
        TaxaAntecipacaoTexto = "2,10";
        MensagemErro = string.Empty;
        ExecutarCalculo();
    }

    private static bool TentarConverterDecimal(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto)) return false;

        string limpo = texto.Trim()
            .Replace("R$", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "")
            .Trim();

        // Se tem ponto e vírgula, ex: 1.000,00
        if (limpo.Contains('.') && limpo.Contains(','))
        {
            limpo = limpo.Replace(".", ""); // remove separador de milhar
        }
        else if (limpo.Contains('.'))
        {
            limpo = limpo.Replace('.', ',');
        }

        return decimal.TryParse(limpo, NumberStyles.Any, CulturaPtBr, out valor);
    }

    private static bool TentarConverterPercentual(string? texto, out decimal taxaDecimal)
    {
        taxaDecimal = 0m;
        if (!TentarConverterDecimal(texto, out decimal valor) || valor < 0)
            return false;

        // Se o usuário digitou 2,19, interpreta como 2,19% (0.0219).
        // Se já digitou 0.0219, interpreta direto como 0.0219.
        taxaDecimal = valor >= 1.0m ? valor / 100.0m : valor;
        return true;
    }
}

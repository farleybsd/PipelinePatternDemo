using PipelinePatternDemo.Pipeline;
using PipelinePatternDemo.Rules;

namespace PipelinePatternDemo.Tests.Rules;

// Testes de cada regra isoladamente, focando nos valores de fronteira
// (é exatamente ali que bugs como o "500 x 580" costumam se esconder).

public class CreditScoreRuleTests
{
    [Theory]
    [InlineData(300, true, false)]
    [InlineData(579, true, false)]
    [InlineData(580, false, true)]
    [InlineData(669, false, true)]
    [InlineData(670, false, false)]
    [InlineData(850, false, false)]
    public async Task Avalia_limites_do_score(int score, bool expectRejection, bool expectWarning)
    {
        var application = TestData.GoodApplication(a => a.CreditScore = score);
        var context = new EligibilityContext(application.ApplicationId);

        await new CreditScoreRule().EvaluateAsync(application, context);

        Assert.Equal(expectRejection, context.IsRejected);
        Assert.Equal(expectWarning, context.HasWarnings);
    }

    [Fact]
    public async Task Mensagem_de_reprovacao_informa_o_minimo_real()
    {
        var application = TestData.GoodApplication(a => a.CreditScore = 550);
        var context = new EligibilityContext(application.ApplicationId);

        await new CreditScoreRule().EvaluateAsync(application, context);

        Assert.Contains("580", Assert.Single(context.RejectionReasons));
    }
}

public class IncomeValidationRuleTests
{
    // Empréstimo de 100.000 => renda mínima 30.000; abaixo de 45.000 (1,5x) gera aviso.
    [Theory]
    [InlineData(29_999, true, false)]
    [InlineData(30_000, false, true)]
    [InlineData(44_999, false, true)]
    [InlineData(45_000, false, false)]
    public async Task Avalia_renda_minima(decimal income, bool expectRejection, bool expectWarning)
    {
        var application = TestData.GoodApplication(a =>
        {
            a.RequestedLoanAmount = 100_000;
            a.AnnualIncome = income;
        });
        var context = new EligibilityContext(application.ApplicationId);

        await new IncomeValidationRule().EvaluateAsync(application, context);

        Assert.Equal(expectRejection, context.IsRejected);
        Assert.Equal(expectWarning, context.HasWarnings);
    }
}

public class EmploymentHistoryRuleTests
{
    [Theory]
    [InlineData(0, true, false)]
    [InlineData(0.9, true, false)]
    [InlineData(1, false, true)]
    [InlineData(1.9, false, true)]
    [InlineData(2, false, false)]
    public async Task Avalia_tempo_de_emprego(double years, bool expectRejection, bool expectWarning)
    {
        var application = TestData.GoodApplication(a => a.EmploymentYears = years);
        var context = new EligibilityContext(application.ApplicationId);

        await new EmploymentHistoryRule().EvaluateAsync(application, context);

        Assert.Equal(expectRejection, context.IsRejected);
        Assert.Equal(expectWarning, context.HasWarnings);
    }
}

public class DebtToIncomeRatioRuleTests
{
    // Renda mensal 10.000 e empréstimo zerado: o DTI fica igual a MonthlyDebt / 10.000.
    [Theory]
    [InlineData(3_600, false, false)] // 36% exato: ainda ok
    [InlineData(3_700, false, true)]  // 37%: aviso
    [InlineData(4_300, false, true)]  // 43% exato: ainda só aviso
    [InlineData(4_400, true, false)]  // 44%: reprova
    public async Task Avalia_indice_de_endividamento(decimal monthlyDebt, bool expectRejection, bool expectWarning)
    {
        var application = TestData.GoodApplication(a =>
        {
            a.AnnualIncome = 120_000;
            a.RequestedLoanAmount = 0;
            a.MonthlyDebt = monthlyDebt;
        });
        var context = new EligibilityContext(application.ApplicationId);

        await new DebtToIncomeRatioRule().EvaluateAsync(application, context);

        Assert.Equal(expectRejection, context.IsRejected);
        Assert.Equal(expectWarning, context.HasWarnings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Renda_zero_ou_negativa_reprova_sem_dividir_por_zero(decimal income)
    {
        var application = TestData.GoodApplication(a => a.AnnualIncome = income);
        var context = new EligibilityContext(application.ApplicationId);

        await new DebtToIncomeRatioRule().EvaluateAsync(application, context);

        Assert.True(context.IsRejected);
        Assert.False(context.Data.ContainsKey("DTIRatio"));
    }
}

public class FraudCheckRuleTests
{
    [Fact]
    public async Task Proposta_sinalizada_e_reprovada()
    {
        var application = TestData.GoodApplication(a => a.IsFlaggedForFraud = true);
        var context = new EligibilityContext(application.ApplicationId);

        await new FraudCheckRule().EvaluateAsync(application, context);

        Assert.True(context.IsRejected);
    }

    [Fact]
    public async Task Emprestimo_acima_de_2x_a_renda_gera_aviso()
    {
        var application = TestData.GoodApplication(a =>
        {
            a.AnnualIncome = 100_000;
            a.RequestedLoanAmount = 200_001;
        });
        var context = new EligibilityContext(application.ApplicationId);

        await new FraudCheckRule().EvaluateAsync(application, context);

        Assert.False(context.IsRejected);
        Assert.True(context.HasWarnings);
    }

    [Fact]
    public async Task Proposta_normal_passa_sem_ressalvas()
    {
        var application = TestData.GoodApplication();
        var context = new EligibilityContext(application.ApplicationId);

        await new FraudCheckRule().EvaluateAsync(application, context);

        Assert.False(context.IsRejected);
        Assert.False(context.HasWarnings);
    }
}

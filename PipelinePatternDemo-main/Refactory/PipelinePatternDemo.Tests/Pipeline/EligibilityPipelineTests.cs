using Microsoft.Extensions.Logging.Abstractions;
using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;
using PipelinePatternDemo.Rules;

namespace PipelinePatternDemo.Tests.Pipeline;

// Testes do "maestro": ordem de execução, curto-circuito, falha técnica e cancelamento.
// Usamos regras falsas (FakeRule) para controlar exatamente o que cada etapa faz.
public class EligibilityPipelineTests
{
    private static EligibilityPipeline CreatePipeline(params IEligibilityRule[] rules)
        => new(rules, NullLogger<EligibilityPipeline>.Instance);

    [Fact]
    public async Task Executa_regras_pelo_Order_e_nao_pela_ordem_de_registro()
    {
        var executed = new List<string>();
        var pipeline = CreatePipeline(
            new FakeRule("C", 3, executed),
            new FakeRule("A", 1, executed),
            new FakeRule("B", 2, executed));

        await pipeline.ExecuteAsync(TestData.GoodApplication());

        Assert.Equal(["A", "B", "C"], executed);
    }

    [Fact]
    public async Task Para_na_primeira_reprovacao()
    {
        var executed = new List<string>();
        var pipeline = CreatePipeline(
            new FakeRule("A", 1, executed),
            new FakeRule("B", 2, executed, (_, ctx) => ctx.AddRejection("reprovado em B")),
            new FakeRule("C", 3, executed));

        var result = await pipeline.ExecuteAsync(TestData.GoodApplication());

        Assert.Equal(["A", "B"], executed);
        Assert.Equal(EligibilityStatus.Rejected, result.Status);
        Assert.False(result.IsApproved);
        Assert.Equal("reprovado em B", Assert.Single(result.RejectionReasons));
    }

    [Fact]
    public async Task Avisos_nao_impedem_aprovacao()
    {
        var pipeline = CreatePipeline(
            new FakeRule("A", 1, null, (_, ctx) => ctx.AddWarning("atenção")));

        var result = await pipeline.ExecuteAsync(TestData.GoodApplication());

        Assert.Equal(EligibilityStatus.Approved, result.Status);
        Assert.True(result.IsApproved);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public async Task Falha_tecnica_interrompe_e_deixa_pendente_de_revisao()
    {
        var executed = new List<string>();
        var pipeline = CreatePipeline(
            new FakeRule("A", 1, executed),
            new FakeRule("B", 2, executed, (_, _) => throw new InvalidOperationException("bureau fora do ar")),
            new FakeRule("C", 3, executed));

        var result = await pipeline.ExecuteAsync(TestData.GoodApplication());

        Assert.Equal(["A", "B"], executed);
        Assert.Equal(EligibilityStatus.PendingReview, result.Status);
        Assert.False(result.IsApproved);
        Assert.Empty(result.RejectionReasons);
        Assert.Single(result.SystemErrors);
    }

    [Fact]
    public async Task Cancelamento_propaga_e_nao_vira_erro_de_regra()
    {
        using var cts = new CancellationTokenSource();
        var executed = new List<string>();
        var pipeline = CreatePipeline(
            new FakeRule("A", 1, executed, (_, _) => cts.Cancel()),
            new FakeRule("B", 2, executed));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pipeline.ExecuteAsync(TestData.GoodApplication(), cts.Token));

        Assert.Equal(["A"], executed);
    }

    [Fact]
    public async Task Pipeline_real_aprova_perfil_excelente()
    {
        var pipeline = CreatePipeline(
            new FraudCheckRule(),
            new DebtToIncomeRatioRule(),
            new EmploymentHistoryRule(),
            new IncomeValidationRule(),
            new CreditScoreRule());

        var result = await pipeline.ExecuteAsync(TestData.GoodApplication());

        Assert.Equal(EligibilityStatus.Approved, result.Status);
        Assert.Empty(result.Warnings);
    }

    // Regra de mentira: anota que foi executada e, opcionalmente, faz alguma ação no contexto.
    private sealed class FakeRule(
        string name,
        int order,
        List<string>? executed,
        Action<LoanApplication, EligibilityContext>? action = null) : IEligibilityRule
    {
        public string RuleName => name;
        public int Order => order;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context, CancellationToken cancellationToken = default)
        {
            executed?.Add(name);
            action?.Invoke(application, context);
            return Task.CompletedTask;
        }
    }
}

using PipelinePatternDemo.Models;

namespace PipelinePatternDemo.Tests;

// Fábrica de propostas para os testes.
// Parte de um perfil "perfeito" (passa em todas as regras sem aviso) e cada teste
// muda só o campo que interessa — assim fica claro o que está sendo testado.
internal static class TestData
{
    public static LoanApplication GoodApplication(Action<LoanApplication>? customize = null)
    {
        var application = new LoanApplication
        {
            ApplicantName = "Cliente Teste",
            CreditScore = 780,
            AnnualIncome = 120_000,
            MonthlyDebt = 1_000,
            RequestedLoanAmount = 200_000,
            EmploymentYears = 6,
            HasExistingLoans = false,
            IsFlaggedForFraud = false
        };

        customize?.Invoke(application);
        return application;
    }
}

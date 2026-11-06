using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    // Quarta etapa: o famoso índice de endividamento (DTI - Debt-to-Income).
    // Somamos as dívidas que o cliente já tem com uma estimativa da nova parcela
    // e comparamos com a renda mensal. Acima de 43% reprova; acima de 36% gera aviso.
    public class DebtToIncomeRatioRule : IEligibilityRule
    {
        public string RuleName => "Verificação de Índice de Endividamento (DTI)";

        public int Order => 4;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context, CancellationToken cancellationToken = default)
        {
            // Proteção extra: sem renda não existe índice de endividamento (e evitamos dividir por zero).
            // A API já barra isso na validação, mas a regra não deve depender de quem a chama.
            if (application.AnnualIncome <= 0)
            {
                context.AddRejection("Não é possível calcular o índice de endividamento sem renda anual informada");
                return Task.CompletedTask;
            }

            var monthlyIncome = application.AnnualIncome / 12;

            // Estimativa simplificada da parcela: 5% do valor ao ano, dividido em 12 meses.
            var estimatedMonthlyPayment = (application.RequestedLoanAmount * 0.05m) / 12;
            var totalMonthlyDebt = application.MonthlyDebt + estimatedMonthlyPayment;
            var dtiRatio = totalMonthlyDebt / monthlyIncome;

            // Guardamos o índice no contexto caso outra regra queira usá-lo depois.
            context.SetData("DTIRatio", dtiRatio);

            if (dtiRatio > 0.43m)
            {
                context.AddRejection(
                    $"Índice de endividamento de {dtiRatio:P2} ultrapassa o máximo permitido de 43%. " +
                    $"Renda mensal: ${monthlyIncome:N2}, Dívida mensal total: ${totalMonthlyDebt:N2}"
                );
            }
            else if (dtiRatio > 0.36m)
            {
                context.AddWarning($"Índice de endividamento de {dtiRatio:P2} está elevado. Considere um valor de empréstimo menor");
            }

            return Task.CompletedTask;
        }
    }
}

using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    // Segunda etapa: a renda do cliente aguenta o tamanho do empréstimo?
    // A regra exige uma renda anual de pelo menos 30% do valor pedido.
    // Se a renda estiver só um pouco acima desse mínimo, deixamos um aviso.
    public class IncomeValidationRule : IEligibilityRule
    {
        public string RuleName => "Validação de Renda";

        public int Order => 2;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context, CancellationToken cancellationToken = default)
        {
            // Renda mínima esperada: 30% do valor solicitado.
            var minAnnualIncome = application.RequestedLoanAmount * 0.3m;

            if (application.AnnualIncome < minAnnualIncome)
            {
                context.AddRejection(
                    $"Renda anual de ${application.AnnualIncome:N2} é insuficiente para um empréstimo de ${application.RequestedLoanAmount:N2}. " +
                    $"Mínimo exigido: ${minAnnualIncome:N2}"
                );
            }
            else if (application.AnnualIncome < minAnnualIncome * 1.5m)
            {
                context.AddWarning("Renda está no limite para o valor de empréstimo solicitado");
            }

            context.SetData("Income", minAnnualIncome);

            return Task.CompletedTask;
        }
    }
}

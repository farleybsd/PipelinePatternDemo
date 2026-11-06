using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    // Última etapa: checagem de fraude.
    // Se algum sistema externo já marcou a proposta como suspeita, reprovamos e pedimos revisão manual.
    // Também desconfiamos de pedidos muito acima da renda (mais que o dobro do salário anual).
    public class FraudCheckRule : IEligibilityRule
    {
        public string RuleName => "Verificação de Fraude";

        public int Order => 5;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context, CancellationToken cancellationToken = default)
        {
            if (application.IsFlaggedForFraud)
            {
                context.AddRejection("A proposta foi sinalizada como possível fraude. Revisão manual necessária");
            }

            if (application.RequestedLoanAmount > application.AnnualIncome * 2)
            {
                context.AddWarning("Valor do empréstimo ultrapassa 2x a renda anual. Verificação adicional necessária");
            }

            return Task.CompletedTask;
        }
    }
}

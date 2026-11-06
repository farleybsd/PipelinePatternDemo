using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    // Terceira etapa: estabilidade no emprego.
    // Menos de 1 ano de trabalho reprova; entre 1 e 2 anos passa,
    // mas avisamos que a proposta pode precisar de uma análise mais cuidadosa.
    public class EmploymentHistoryRule : IEligibilityRule
    {
        public string RuleName => "Verificação de Histórico de Emprego";

        public int Order => 3;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context, CancellationToken cancellationToken = default)
        {
            if (application.EmploymentYears < 1)
            {
                context.AddRejection("É necessário estar empregado há pelo menos 1 ano");
            }
            else if (application.EmploymentYears < 2)
            {
                context.AddWarning("Menos de 2 anos de histórico de emprego. A proposta pode precisar de análise adicional");
            }

            context.SetData("EmploymentYears", application.EmploymentYears);

            return Task.CompletedTask;
        }
    }
}

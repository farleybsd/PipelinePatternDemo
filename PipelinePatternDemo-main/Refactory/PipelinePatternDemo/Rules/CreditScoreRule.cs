using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    // Primeira etapa da análise: olhar o score de crédito do cliente.
    // Score muito baixo reprova na hora; score mediano passa, mas com um aviso
    // de que os juros provavelmente serão mais altos.
    public class CreditScoreRule : IEligibilityRule
    {
        // Abaixo disso, a proposta é reprovada.
        private const int MinimumCreditScore = 580;

        // Abaixo disso (mas acima do mínimo), a proposta passa com aviso.
        private const int PreferredCreditScore = 670;

        public string RuleName => "Validação de Score de Crédito";

        public int Order => 1;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context, CancellationToken cancellationToken = default)
        {
            // O teste e a mensagem usam a mesma constante, assim os dois nunca mais ficam desencontrados.
            if (application.CreditScore < MinimumCreditScore)
            {
                context.AddRejection($"Score de crédito {application.CreditScore} é muito baixo. Mínimo exigido: {MinimumCreditScore}");
            }
            else if (application.CreditScore < PreferredCreditScore)
            {
                context.AddWarning($"Score de crédito {application.CreditScore} está abaixo do nível desejado. Espere taxas de juros mais altas");
            }

            context.SetData("CreditScore", application.CreditScore);

            return Task.CompletedTask;
        }
    }
}

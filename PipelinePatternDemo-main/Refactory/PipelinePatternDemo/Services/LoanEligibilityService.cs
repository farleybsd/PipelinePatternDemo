using PipelinePatternDemo.Models;
using PipelinePatternDemo.Rules;

namespace PipelinePatternDemo.Services
{
    // Esta é a "porta de entrada" para quem quer avaliar um empréstimo.
    // Ela esconde os detalhes do pipeline: quem chama não precisa saber quantas regras existem
    // nem em que ordem rodam — só entrega a proposta e recebe a decisão de volta (padrão Facade).
    public class LoanEligibilityService(EligibilityPipeline pipeline)
    {
        // Recebe a proposta e repassa para o pipeline fazer o trabalho pesado.
        public Task<EligibilityResult> EvaluateApplicationAsync(LoanApplication application, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(application);

            return pipeline.ExecuteAsync(application, cancellationToken);
        }
    }
}

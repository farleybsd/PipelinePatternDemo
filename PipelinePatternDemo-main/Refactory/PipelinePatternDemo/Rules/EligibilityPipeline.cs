using Microsoft.Extensions.Logging;
using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    // Este é o "maestro" da análise. Ele recebe todas as regras registradas no DI,
    // coloca cada uma na sua vez (pelo Order) e passa a proposta por elas, uma de cada vez.
    // Se alguma regra reprovar, ele para por ali — não faz sentido continuar analisando
    // uma proposta que já foi recusada. Se alguma regra quebrar, ele também para,
    // mas marca a proposta como "pendente de revisão" em vez de recusá-la.
    //
    // Toda a observabilidade sai pelo ILogger (logs estruturados); o pipeline não imprime nada
    // no console. Quem quiser exibir o resultado para uma pessoa faz isso a partir do EligibilityResult.
    public class EligibilityPipeline(
        IEnumerable<IEligibilityRule> rules,
        ILogger<EligibilityPipeline> logger
        )
    {
        public async Task<EligibilityResult> ExecuteAsync(LoanApplication application, CancellationToken cancellationToken = default)
        {
            var context = new EligibilityContext(application.ApplicationId);

            // Todo log dentro deste bloco carrega o ApplicationId automaticamente.
            // De propósito, não logamos nome nem renda do cliente: são dados pessoais (LGPD).
            using var _ = logger.BeginScope(new Dictionary<string, object>
            {
                ["ApplicationId"] = application.ApplicationId
            });

            logger.LogInformation("Iniciando pipeline de elegibilidade para a proposta {ApplicationId}", application.ApplicationId);

            var stopOnRejection = true;

            // Ordenamos pelo Order para que a sequência dependa da própria regra,
            // e não da ordem em que alguém lembrou de registrá-la no DI.
            foreach (var rule in rules.OrderBy(r => r.Order))
            {
                // Se quem chamou desistiu (ex.: a requisição HTTP foi cancelada), não começamos a próxima regra.
                cancellationToken.ThrowIfCancellationRequested();

                var warningsBefore = context.Warnings.Count;
                var rejectionsBefore = context.RejectionReasons.Count;

                try
                {
                    logger.LogDebug("Avaliando regra {RuleName} (Order {Order})", rule.RuleName, rule.Order);
                    await rule.EvaluateAsync(application, context, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Cancelamento não é erro de regra: deixamos a exceção subir para quem pediu o cancelamento.
                    throw;
                }
                catch (Exception ex)
                {
                    // Uma falha técnica não pode nem aprovar nem recusar a proposta "sem querer".
                    // Registramos o erro, interrompemos o pipeline (a decisão ficaria incompleta)
                    // e o resultado sai como PendingReview.
                    logger.LogError(ex, "Erro ao executar a regra {RuleName}. Pipeline interrompido", rule.RuleName);
                    context.AddSystemError($"Não foi possível concluir a etapa '{rule.RuleName}'. A proposta será revisada");
                    break;
                }

                logger.LogInformation(
                    "Regra {RuleName} concluída: {NewWarnings} aviso(s), {NewRejections} reprovação(ões)",
                    rule.RuleName,
                    context.Warnings.Count - warningsBefore,
                    context.RejectionReasons.Count - rejectionsBefore);

                // Se a regra reprovou, encerramos o pipeline aqui mesmo.
                if (stopOnRejection && context.IsRejected)
                {
                    logger.LogWarning("Proposta {ApplicationId} reprovada pela regra {RuleName}", application.ApplicationId, rule.RuleName);
                    break;
                }
            }

            var result = new EligibilityResult
            {
                ApplicationId = application.ApplicationId,
                Status = DetermineStatus(context),
                RejectionReasons = context.RejectionReasons,
                Warnings = context.Warnings,
                SystemErrors = context.SystemErrors,
                ContextData = context.Data
            };

            logger.LogInformation(
                "Pipeline finalizado para a proposta {ApplicationId} com status {Status}",
                result.ApplicationId,
                result.Status);

            return result;
        }

        // Erro técnico tem prioridade: se a análise não terminou, não dá para afirmar nada.
        private static EligibilityStatus DetermineStatus(EligibilityContext context)
        {
            if (context.HasSystemErrors) return EligibilityStatus.PendingReview;
            if (context.IsRejected) return EligibilityStatus.Rejected;
            return EligibilityStatus.Approved;
        }
    }

    // O "laudo final" da análise: qual o desfecho, por quê, e quais pontos de atenção surgiram.
    // É isso que volta para quem pediu a avaliação (console ou API).
    public class EligibilityResult
    {
        public Guid ApplicationId { get; set; }
        public EligibilityStatus Status { get; set; }

        // Atalho mantido por compatibilidade: só é aprovado quando o status é Approved.
        public bool IsApproved => Status == EligibilityStatus.Approved;

        public List<string> RejectionReasons { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> SystemErrors { get; set; } = new();
        public Dictionary<string, object> ContextData { get; set; } = new();
    }
}

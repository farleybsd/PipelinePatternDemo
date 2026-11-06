using PipelinePatternDemo.Models;

namespace PipelinePatternDemo.Pipeline
{
    // Este é o "contrato" que toda regra de elegibilidade precisa assinar.
    // A ideia é simples: cada regra olha para a proposta, faz sua análise e anota
    // o que encontrou no contexto compartilhado. Assim, para criar uma regra nova,
    // basta implementar esta interface e registrá-la no DI — o pipeline não muda nada.
    public interface IEligibilityRule
    {
        // Um nome amigável para a regra, usado nos logs e nas mensagens.
        string RuleName { get; }

        // Define a posição da regra na fila: números menores rodam primeiro.
        // O pipeline ordena as regras por este valor antes de executá-las.
        int Order { get; }

        // Onde a mágica acontece: a regra avalia a proposta e registra avisos ou reprovações no contexto.
        // O CancellationToken permite abortar a análise se o cliente desistir da requisição
        // (importante quando uma regra chama serviços externos, como bureau de crédito).
        Task EvaluateAsync(LoanApplication application, EligibilityContext context, CancellationToken cancellationToken = default);
    }
}

namespace PipelinePatternDemo.Api.DTOs;

// A resposta que a API devolve para o front-end: a decisão e as explicações.
// Repare que não expomos o ContextData interno do pipeline — o cliente só vê o que precisa.
public class LoanEvaluationResponse
{
    public Guid ApplicationId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;

    // "Approved", "Rejected" ou "PendingReview" (falha técnica, a proposta vai para revisão).
    public string Status { get; set; } = string.Empty;

    // Mantido para compatibilidade com clientes antigos: true apenas quando Status = "Approved".
    public bool IsApproved { get; set; }

    public List<string> RejectionReasons { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> SystemErrors { get; set; } = new();
}

namespace PipelinePatternDemo.Pipeline
{
    // Os três desfechos possíveis de uma análise.
    // O mais importante aqui é separar "o negócio disse não" (Rejected) de
    // "o sistema não conseguiu terminar a análise" (PendingReview): uma falha técnica
    // nunca deve virar uma recusa para o cliente — ela vai para revisão ou reprocessamento.
    public enum EligibilityStatus
    {
        // Todas as regras executadas e nenhuma reprovou (pode haver avisos).
        Approved,

        // Pelo menos uma regra de negócio reprovou a proposta.
        Rejected,

        // Alguma regra falhou tecnicamente; a decisão ficou incompleta e precisa de revisão.
        PendingReview
    }
}

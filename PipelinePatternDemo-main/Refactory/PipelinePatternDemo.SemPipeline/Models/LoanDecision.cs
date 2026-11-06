namespace PipelinePatternDemo.SemPipeline.Models
{
    // O resultado da análise. Sem o Pipeline não existe um "contexto" que viaja entre as etapas,
    // então o próprio método gigante vai preenchendo este objeto conforme avança.
    public class LoanDecision
    {
        public Guid ApplicationId { get; set; }
        public bool IsApproved => RejectionReasons.Count == 0;
        public List<string> RejectionReasons { get; } = new();
        public List<string> Warnings { get; } = new();
    }
}

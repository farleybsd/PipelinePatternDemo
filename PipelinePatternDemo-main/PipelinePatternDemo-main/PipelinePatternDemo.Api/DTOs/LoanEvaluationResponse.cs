namespace PipelinePatternDemo.Api.DTOs;

public class LoanEvaluationResponse
{
    public Guid ApplicationId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public List<string> RejectionReasons { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

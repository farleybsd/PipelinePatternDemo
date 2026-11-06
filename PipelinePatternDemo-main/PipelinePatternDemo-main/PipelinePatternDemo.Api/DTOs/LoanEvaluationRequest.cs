namespace PipelinePatternDemo.Api.DTOs;

public class LoanEvaluationRequest
{
    public string ApplicantName { get; set; } = string.Empty;
    public int CreditScore { get; set; }
    public decimal AnnualIncome { get; set; }
    public decimal MonthlyDebt { get; set; }
    public decimal RequestedLoanAmount { get; set; }
    public double EmploymentYears { get; set; }
    public bool HasExistingLoans { get; set; }
    public bool IsFlaggedForFraud { get; set; }
}

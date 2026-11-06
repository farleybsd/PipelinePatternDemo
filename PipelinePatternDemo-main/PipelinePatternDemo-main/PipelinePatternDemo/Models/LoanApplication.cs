namespace PipelinePatternDemo.Models
{
    public class LoanApplication
    {
        public Guid ApplicationId { get; set; } = Guid.NewGuid();
        public string ApplicantName { get; set; } = string.Empty;
        public int CreditScore { get; set; }
        public decimal AnnualIncome { get; set; }
        public decimal MonthlyDebt { get; set; }
        public decimal RequestedLoanAmount { get; set; }
        public double EmploymentYears { get; set; }
        public bool HasExistingLoans { get; set; }
        public bool IsFlaggedForFraud { get; set; }
        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;
using PipelinePatternDemo.Rules;
using PipelinePatternDemo.Services;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

services.AddScoped<IEligibilityRule, CreditScoreRule>();
services.AddScoped<IEligibilityRule, IncomeValidationRule>();
services.AddScoped<IEligibilityRule, EmploymentHistoryRule>();
services.AddScoped<IEligibilityRule, DebtToIncomeRatioRule>();
services.AddScoped<IEligibilityRule, FraudCheckRule>();

services.AddScoped<EligibilityPipeline>();

services.AddScoped<LoanEligibilityService>();

var serviceProvider = services.BuildServiceProvider();

var testApplications = new[]
{
    // Application 1: Should be rejected (low credit score, low income)
    new LoanApplication
    {
        ApplicantName = "John Doe",
        CreditScore = 550,
        AnnualIncome = 30_000,
        MonthlyDebt = 500,
        RequestedLoanAmount = 250_000,
        EmploymentYears = 3,
        HasExistingLoans = true,
        IsFlaggedForFraud = false
    },
    
    // Application 2: Should be rejected (flagged for fraud)
    new LoanApplication
    {
        ApplicantName = "Jane Smith",
        CreditScore = 720,
        AnnualIncome = 85_000,
        MonthlyDebt = 1_200,
        RequestedLoanAmount = 200_000,
        EmploymentYears = 5,
        HasExistingLoans = true,
        IsFlaggedForFraud = true
    },
    
    // Application 3: Should be approved with warnings
    new LoanApplication
    {
        ApplicantName = "Bob Johnson",
        CreditScore = 650,
        AnnualIncome = 75_000,
        MonthlyDebt = 800,
        RequestedLoanAmount = 150_000,
        EmploymentYears = 1.5,
        HasExistingLoans = false,
        IsFlaggedForFraud = false
    },
    
    // Application 4: Should be fully approved
    new LoanApplication
    {
        ApplicantName = "Alice Brown",
        CreditScore = 780,
        AnnualIncome = 120_000,
        MonthlyDebt = 1_000,
        RequestedLoanAmount = 200_000,
        EmploymentYears = 6,
        HasExistingLoans = false,
        IsFlaggedForFraud = false
    }
};

using var scope = serviceProvider.CreateScope();
var loanService = scope.ServiceProvider.GetRequiredService<LoanEligibilityService>();

Console.WriteLine("🏦 LOAN ELIGIBILITY SYSTEM DEMO");
Console.WriteLine("================================\n");
Console.WriteLine("Pattern: Pipeline Pattern with .NET 10 DI");
Console.WriteLine("================================\n");

foreach (var application in testApplications)
{
    Console.WriteLine($"\n{'=' * 70}");
    Console.WriteLine($"Processing Application for: {application.ApplicantName}");
    Console.WriteLine($"Requested Amount: ${application.RequestedLoanAmount:N2}");
    Console.WriteLine($"Credit Score: {application.CreditScore}");
    Console.WriteLine($"Annual Income: ${application.AnnualIncome:N2}");
    Console.WriteLine($"{'=' * 70}");

    var result = await loanService.EvaluateApplicationAsync(application);

    Console.WriteLine($"\nFinal Decision: {(result.IsApproved ? "APPROVED ✅" : "REJECTED ❌")}");

    // Add a delay between applications for readability
    if (application != testApplications.Last())
    {
        Console.WriteLine("\nPress any key to process next application...");
        Console.ReadKey();
        Console.Clear();
    }
}

Console.WriteLine("\n========================================");
Console.WriteLine("All applications processed successfully!");
Console.WriteLine("========================================");

// Display pattern comparison
Console.WriteLine(@"

📊 Pattern Comparison:
┌─────────────────────┬──────────────────┬─────────────────────┬──────────────────────┐
│ Pattern             │ Pipeline         │ Strategy            │ Chain of Resp.      │
├─────────────────────┼──────────────────┼─────────────────────┼──────────────────────┤
│ Execution           │ All steps run    │ One implementation  │ Stops at first       │
│                     │ sequentially     │ chosen at runtime   │ handler that handles │
├─────────────────────┼──────────────────┼─────────────────────┼──────────────────────┤
│ State Sharing       │ Context object   │ No shared state     │ No shared state      │
│                     │ shared across    │ by default          │ by default           │
├─────────────────────┼──────────────────┼─────────────────────┼──────────────────────┤
│ Use Case            │ Multi-step       │ Algorithm selection │ Request processing   │
│                     │ validation       │ (payment methods)   │ (middleware)         │
├─────────────────────┼──────────────────┼─────────────────────┼──────────────────────┤
│ Open/Closed         │ Add new steps    │ Add new strategies  │ Add new handlers     │
│ Principle           │ easily           │ easily              │ easily               │
└─────────────────────┴──────────────────┴─────────────────────┴──────────────────────┘
");
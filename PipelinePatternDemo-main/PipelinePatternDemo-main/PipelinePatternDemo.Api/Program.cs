using Scalar.AspNetCore;
using PipelinePatternDemo.Api.DTOs;
using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;
using PipelinePatternDemo.Rules;
using PipelinePatternDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddScoped<IEligibilityRule, CreditScoreRule>();
builder.Services.AddScoped<IEligibilityRule, IncomeValidationRule>();
builder.Services.AddScoped<IEligibilityRule, EmploymentHistoryRule>();
builder.Services.AddScoped<IEligibilityRule, DebtToIncomeRatioRule>();
builder.Services.AddScoped<IEligibilityRule, FraudCheckRule>();

builder.Services.AddScoped<EligibilityPipeline>();
builder.Services.AddScoped<LoanEligibilityService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/loan-evaluation", async (LoanEvaluationRequest request, LoanEligibilityService loanService) =>
{
    var application = new LoanApplication
    {
        ApplicantName = request.ApplicantName,
        CreditScore = request.CreditScore,
        AnnualIncome = request.AnnualIncome,
        MonthlyDebt = request.MonthlyDebt,
        RequestedLoanAmount = request.RequestedLoanAmount,
        EmploymentYears = request.EmploymentYears,
        HasExistingLoans = request.HasExistingLoans,
        IsFlaggedForFraud = request.IsFlaggedForFraud
    };

    var result = await loanService.EvaluateApplicationAsync(application);

    return Results.Ok(new LoanEvaluationResponse
    {
        ApplicationId = result.ApplicationId,
        ApplicantName = request.ApplicantName,
        IsApproved = result.IsApproved,
        RejectionReasons = result.RejectionReasons,
        Warnings = result.Warnings
    });
})
.WithName("EvaluateLoan");

app.Run();

// Versão Web da demo: uma Minimal API que expõe o mesmo pipeline do console.
// O front-end (wwwroot) envia a proposta via POST, a gente valida, converte para o modelo de domínio,
// roda o pipeline e devolve um resumo da decisão em JSON.
using Scalar.AspNetCore;
using PipelinePatternDemo.Api.DTOs;
using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;
using PipelinePatternDemo.Rules;
using PipelinePatternDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Liga a validação automática por DataAnnotations nos parâmetros dos endpoints.
// Se o corpo da requisição for inválido, a API responde 400 (ValidationProblem) sem executar o handler.
builder.Services.AddValidation();

// Padroniza respostas de erro no formato ProblemDetails (RFC 9457).
builder.Services.AddProblemDetails();

// Mesmas regras do console. A ordem de execução é definida pelo Order de cada regra.
builder.Services.AddScoped<IEligibilityRule, CreditScoreRule>();
builder.Services.AddScoped<IEligibilityRule, IncomeValidationRule>();
builder.Services.AddScoped<IEligibilityRule, EmploymentHistoryRule>();
builder.Services.AddScoped<IEligibilityRule, DebtToIncomeRatioRule>();
builder.Services.AddScoped<IEligibilityRule, FraudCheckRule>();

builder.Services.AddScoped<EligibilityPipeline>();
builder.Services.AddScoped<LoanEligibilityService>();

var app = builder.Build();

// Qualquer exceção não tratada vira um 500 em ProblemDetails, sem vazar stack trace para o cliente.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Documentação interativa da API (Scalar) só em ambiente de desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/loan-evaluation", async (
    LoanEvaluationRequest request,
    LoanEligibilityService loanService,
    CancellationToken cancellationToken) =>
{
    // Chegando aqui, o request já passou pela validação das DataAnnotations.
    // Traduzimos o DTO que veio da web para o modelo de domínio que o pipeline entende.
    var application = new LoanApplication
    {
        ApplicantName = request.ApplicantName.Trim(),
        CreditScore = request.CreditScore,
        AnnualIncome = request.AnnualIncome,
        MonthlyDebt = request.MonthlyDebt,
        RequestedLoanAmount = request.RequestedLoanAmount,
        EmploymentYears = request.EmploymentYears,
        HasExistingLoans = request.HasExistingLoans,
        IsFlaggedForFraud = request.IsFlaggedForFraud
    };

    // O CancellationToken da requisição chega até as regras: se o cliente desistir, paramos de trabalhar.
    var result = await loanService.EvaluateApplicationAsync(application, cancellationToken);

    // Mesmo PendingReview volta 200: a requisição foi processada, só a decisão é que ficou pendente.
    return Results.Ok(new LoanEvaluationResponse
    {
        ApplicationId = result.ApplicationId,
        ApplicantName = application.ApplicantName,
        Status = result.Status.ToString(),
        IsApproved = result.IsApproved,
        RejectionReasons = result.RejectionReasons,
        Warnings = result.Warnings,
        SystemErrors = result.SystemErrors
    });
})
.WithName("EvaluateLoan")
.Produces<LoanEvaluationResponse>()
.ProducesValidationProblem();

app.Run();

// Torna a classe Program visível para os testes de integração (WebApplicationFactory<Program>).
public partial class Program;

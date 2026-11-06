using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PipelinePatternDemo.Api.DTOs;

namespace PipelinePatternDemo.Tests.Api;

// Testes de integração: sobem a API de verdade em memória e fazem requisições HTTP.
// Garantem que validação, pipeline e serialização estão conversando direito.
public class LoanEvaluationApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Endpoint = "/api/loan-evaluation";

    private static LoanEvaluationRequest ValidRequest() => new()
    {
        ApplicantName = "Alice Brown",
        CreditScore = 780,
        AnnualIncome = 120_000,
        MonthlyDebt = 1_000,
        RequestedLoanAmount = 200_000,
        EmploymentYears = 6
    };

    [Fact]
    public async Task Proposta_valida_e_boa_retorna_aprovada()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Endpoint, ValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoanEvaluationResponse>();
        Assert.NotNull(body);
        Assert.Equal("Approved", body.Status);
        Assert.True(body.IsApproved);
    }

    [Fact]
    public async Task Proposta_com_fraude_retorna_reprovada()
    {
        var client = factory.CreateClient();
        var request = ValidRequest();
        request.IsFlaggedForFraud = true;

        var response = await client.PostAsJsonAsync(Endpoint, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoanEvaluationResponse>();
        Assert.NotNull(body);
        Assert.Equal("Rejected", body.Status);
        Assert.NotEmpty(body.RejectionReasons);
    }

    [Fact]
    public async Task Dados_invalidos_retornam_400_com_os_campos_errados()
    {
        var client = factory.CreateClient();
        var request = ValidRequest();
        request.ApplicantName = "";
        request.CreditScore = 900;
        request.AnnualIncome = 0;
        request.EmploymentYears = -1;

        var response = await client.PostAsJsonAsync(Endpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errorKeys = doc.RootElement.GetProperty("errors")
            .EnumerateObject()
            .Select(p => p.Name.ToLowerInvariant())
            .ToList();

        Assert.Contains("applicantname", errorKeys);
        Assert.Contains("creditscore", errorKeys);
        Assert.Contains("annualincome", errorKeys);
        Assert.Contains("employmentyears", errorKeys);
    }
}

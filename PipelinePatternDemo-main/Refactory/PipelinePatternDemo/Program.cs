// Ponto de partida da demo no console.
// Aqui montamos a "caixa de ferramentas" (o container de DI), registramos as regras
// e passamos algumas propostas de exemplo pelo pipeline para ver tudo funcionando.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;
using PipelinePatternDemo.Rules;
using PipelinePatternDemo.Services;

var services = new ServiceCollection();

// O logger do console escreve em segundo plano, então os logs se misturariam com a saída da demo.
// Por padrão deixamos desligado para a leitura ficar limpa; rode com "--logs" para ver os logs estruturados.
var showLogs = args.Contains("--logs");

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(showLogs ? LogLevel.Information : LogLevel.None);
});

// Cada regra é registrada como mais uma implementação de IEligibilityRule.
// A ordem de execução vem da propriedade Order de cada regra, não da ordem destas linhas.
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
    // Proposta 1: deve ser reprovada (score de crédito baixo e renda baixa)
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

    // Proposta 2: deve ser reprovada (sinalizada como fraude)
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

    // Proposta 3: deve ser aprovada com avisos
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

    // Proposta 4: deve ser totalmente aprovada
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

Console.WriteLine("🏦 DEMO DO SISTEMA DE ELEGIBILIDADE DE EMPRÉSTIMO");
Console.WriteLine("================================================\n");
Console.WriteLine("Padrão: Pipeline Pattern com DI do .NET 10");
Console.WriteLine("================================================\n");

var separator = new string('=', 70);

foreach (var application in testApplications)
{
    Console.WriteLine($"\n{separator}");
    Console.WriteLine($"Processando proposta de: {application.ApplicantName}");
    Console.WriteLine($"Valor solicitado: ${application.RequestedLoanAmount:N2}");
    Console.WriteLine($"Score de crédito: {application.CreditScore}");
    Console.WriteLine($"Renda anual: ${application.AnnualIncome:N2}");
    Console.WriteLine(separator);

    var result = await loanService.EvaluateApplicationAsync(application);

    // O pipeline só devolve dados; a apresentação para uma pessoa é responsabilidade de quem chama.
    DisplayResult(result);

    // Uma pausa entre as propostas para facilitar a leitura
    // (pulamos a pausa quando a entrada não é um terminal, ex.: rodando em CI ou com redirecionamento).
    if (application != testApplications.Last() && !Console.IsInputRedirected)
    {
        Console.WriteLine("\nPressione qualquer tecla para processar a próxima proposta...");
        Console.ReadKey();
        Console.Clear();
    }
}

Console.WriteLine("\n========================================");
Console.WriteLine("Todas as propostas foram processadas!");
Console.WriteLine("========================================");

// Comparação rápida entre padrões parecidos
Console.WriteLine(@"

📊 Comparação de Padrões:
┌─────────────────────┬──────────────────┬─────────────────────┬──────────────────────┐
│ Padrão              │ Pipeline         │ Strategy            │ Chain of Resp.       │
├─────────────────────┼──────────────────┼─────────────────────┼──────────────────────┤
│ Execução            │ Todas as etapas  │ Uma implementação   │ Para no primeiro     │
│                     │ em sequência     │ escolhida em runtime│ handler que resolve  │
├─────────────────────┼──────────────────┼─────────────────────┼──────────────────────┤
│ Estado              │ Objeto de        │ Sem estado          │ Sem estado           │
│ compartilhado       │ contexto comum   │ compartilhado       │ compartilhado        │
├─────────────────────┼──────────────────┼─────────────────────┼──────────────────────┤
│ Caso de uso         │ Validação em     │ Escolha de algoritmo│ Processar requisições│
│                     │ várias etapas    │ (meios de pagamento)│ (middleware)         │
├─────────────────────┼──────────────────┼─────────────────────┼──────────────────────┤
│ Aberto/Fechado      │ Novas etapas     │ Novas estratégias   │ Novos handlers       │
│ (OCP)               │ facilmente       │ facilmente          │ facilmente           │
└─────────────────────┴──────────────────┴─────────────────────┴──────────────────────┘
");

// Mostra o veredito final no console, de um jeito fácil de ler.
static void DisplayResult(EligibilityResult result)
{
    Console.WriteLine(new string('-', 60));

    switch (result.Status)
    {
        case EligibilityStatus.Approved:
            Console.WriteLine($"✅ PROPOSTA APROVADA - {result.ApplicationId}");
            if (result.Warnings.Count > 0)
            {
                Console.WriteLine($"\n⚠️  Com {result.Warnings.Count} aviso(s):");
                PrintList(result.Warnings);
            }
            break;

        case EligibilityStatus.Rejected:
            Console.WriteLine($"❌ PROPOSTA REPROVADA - {result.ApplicationId}");
            Console.WriteLine("\nMotivos da reprovação:");
            PrintList(result.RejectionReasons);
            break;

        case EligibilityStatus.PendingReview:
            Console.WriteLine($"⏳ PROPOSTA PENDENTE DE REVISÃO - {result.ApplicationId}");
            Console.WriteLine("\nA análise não pôde ser concluída:");
            PrintList(result.SystemErrors);
            break;
    }
}

static void PrintList(IEnumerable<string> items)
{
    foreach (var item in items)
    {
        Console.WriteLine($"   - {item}");
    }
}

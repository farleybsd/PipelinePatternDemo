// Demo do MESMO sistema de elegibilidade, mas SEM o Pipeline Pattern.
// Mesmas regras, mesmas propostas de exemplo e mesma saída que o projeto PipelinePatternDemo.
// A ideia é você abrir os dois lado a lado e comparar como o código está organizado.
//
// Repare também que aqui não há injeção de dependência: como existe uma classe só,
// basta dar new nela. Simples no começo, mas não há onde "plugar" regras novas.
using PipelinePatternDemo.SemPipeline.Models;
using PipelinePatternDemo.SemPipeline.Services;

var checker = new LoanEligibilityChecker();

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

Console.WriteLine("🏦 DEMO DO SISTEMA DE ELEGIBILIDADE DE EMPRÉSTIMO");
Console.WriteLine("================================================\n");
Console.WriteLine("Implementação: SEM Pipeline Pattern (if/else em uma classe só)");
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

    var decision = checker.Evaluate(application);

    DisplayDecision(decision);

    // Uma pausa entre as propostas para facilitar a leitura (pulada quando a entrada não é um terminal).
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

Console.WriteLine(@"

🤔 E aí, qual a diferença?
┌──────────────────────────────┬──────────────────────────────┬──────────────────────────────┐
│ Situação                     │ Sem Pipeline (este projeto)  │ Com Pipeline                 │
├──────────────────────────────┼──────────────────────────────┼──────────────────────────────┤
│ Adicionar uma regra nova     │ Editar o método gigante      │ Criar uma classe + 1 linha DI│
│ Mudar a ordem das regras     │ Recortar e colar blocos      │ Trocar o valor de Order      │
│ Testar uma regra sozinha     │ Impossível, passa por todas  │ Testa a classe isolada       │
│ Erro técnico em uma etapa    │ Derruba tudo ou try/catch    │ Tratado em um lugar só       │
│                              │ espalhado                    │ (PendingReview)              │
│ Parar na primeira reprovação │ return em cada bloco         │ Um if no pipeline            │
│ Vários devs mexendo ao mesmo │ Conflito no mesmo arquivo    │ Cada um na sua regra         │
│ tempo                        │                              │                              │
├──────────────────────────────┼──────────────────────────────┼──────────────────────────────┤
│ Quando vale a pena           │ Poucas regras, que quase     │ Muitas regras, que mudam com │
│                              │ nunca mudam                  │ frequência ou por produto    │
└──────────────────────────────┴──────────────────────────────┴──────────────────────────────┘
");

// Mostra o veredito final no console.
static void DisplayDecision(LoanDecision decision)
{
    Console.WriteLine(new string('-', 60));

    if (decision.IsApproved)
    {
        Console.WriteLine($"✅ PROPOSTA APROVADA - {decision.ApplicationId}");
        if (decision.Warnings.Count > 0)
        {
            Console.WriteLine($"\n⚠️  Com {decision.Warnings.Count} aviso(s):");
            foreach (var warning in decision.Warnings)
            {
                Console.WriteLine($"   - {warning}");
            }
        }
    }
    else
    {
        Console.WriteLine($"❌ PROPOSTA REPROVADA - {decision.ApplicationId}");
        Console.WriteLine("\nMotivos da reprovação:");
        foreach (var reason in decision.RejectionReasons)
        {
            Console.WriteLine($"   - {reason}");
        }
    }
}

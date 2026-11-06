# Design Patterns usados neste projeto

Este documento explica, sem enrolação, quais padrões de projeto aparecem neste código, onde eles estão e por que foram escolhidos. No fim há a lista das melhorias aplicadas.

---

## 1. Pipeline Pattern (o protagonista)

### A ideia

Imagine uma linha de montagem: a peça passa por várias estações, uma depois da outra, e cada estação faz uma verificação específica. No nosso caso, a "peça" é a proposta de empréstimo e as "estações" são as regras de elegibilidade.

```
LoanApplication ──► [Score] ──► [Renda] ──► [Emprego] ──► [DTI] ──► [Fraude] ──► EligibilityResult
                       │           │            │           │           │
                       └───────────┴────────────┴───────────┴───────────┘
                                   todas escrevem no EligibilityContext
```

### Onde está no código

| Papel no padrão          | Classe                                   |
|--------------------------|------------------------------------------|
| Contrato de cada etapa   | `Pipeline/IEligibilityRule.cs`           |
| Etapas concretas         | `Rules/*Rule.cs` (5 regras)              |
| Estado compartilhado     | `Pipeline/EligibilityContext.cs`         |
| Orquestrador             | `Rules/EligibilityPipeline.cs`           |
| Resultado final          | `EligibilityResult` (dentro de `EligibilityPipeline.cs`) |

### Por que usar

- **Cada regra faz uma coisa só.** A regra de score não sabe nada sobre fraude, e vice-versa. Isso é o *Single Responsibility Principle* na prática.
- **Adicionar regra nova não exige mexer no pipeline.** É só criar a classe que implementa `IEligibilityRule`, dar um `Order` e registrar no DI. Isso é o *Open/Closed Principle*.
- **Fácil de testar.** Cada regra pode ser testada isoladamente, passando uma `LoanApplication` e um `EligibilityContext`.
- **Ordem explícita.** A propriedade `Order` diz quem roda primeiro, e o pipeline respeita isso com `OrderBy(r => r.Order)`.
- **Curto-circuito.** Com `stopOnRejection = true`, assim que uma regra reprova, as demais nem são executadas — economiza processamento e deixa o motivo da recusa bem claro.

---

## 2. Context Object

O `EligibilityContext` é um objeto que viaja junto com a proposta por todas as etapas. Ele guarda:

- `Warnings` — pontos de atenção que não impedem a aprovação;
- `RejectionReasons` — motivos de reprovação;
- `Data` — um dicionário livre para uma regra deixar informação para outra (por exemplo, o `DTIRatio`).

Sem ele, cada regra teria que retornar um tipo diferente e o pipeline precisaria saber juntar tudo. Com ele, o contrato fica simples: "avalie e anote no contexto".

---

## 3. Strategy (aplicado a cada regra)

Cada classe de regra é, na essência, uma *estratégia* de validação intercambiável por trás da mesma interface (`IEligibilityRule`). A diferença para o Strategy clássico é que aqui **todas** as estratégias são executadas, e não apenas uma escolhida em tempo de execução.

---

## 4. Facade

O `LoanEligibilityService` é uma fachada: quem usa (o `Program.cs` do console ou o endpoint da API) só chama `EvaluateApplicationAsync(application)` e recebe o resultado. Não precisa saber que existe pipeline, contexto, cinco regras etc. Se amanhã a análise mudar por dentro, quem chama não percebe.

---

## 5. Dependency Injection (Inversão de Controle)

As regras são registradas assim:

```csharp
services.AddScoped<IEligibilityRule, CreditScoreRule>();
services.AddScoped<IEligibilityRule, IncomeValidationRule>();
// ...
```

E o pipeline recebe todas de uma vez, pelo construtor primário:

```csharp
public class EligibilityPipeline(IEnumerable<IEligibilityRule> rules, ILogger<EligibilityPipeline> logger)
```

O pipeline não dá `new` em nenhuma regra. Ele só depende da abstração. Isso permite trocar, remover ou adicionar regras apenas mexendo no registro do DI.

---

## 6. DTO (Data Transfer Object)

Na API, `LoanEvaluationRequest` e `LoanEvaluationResponse` separam o "formato da web" do modelo de domínio (`LoanApplication` / `EligibilityResult`). Assim a tela pode mudar sem quebrar as regras de negócio, e dados internos (como `ContextData`) não vazam para o cliente.

---

## Pipeline x Strategy x Chain of Responsibility

| Aspecto               | Pipeline                         | Strategy                          | Chain of Responsibility             |
|-----------------------|----------------------------------|-----------------------------------|-------------------------------------|
| Execução              | Todas as etapas, em sequência    | Uma implementação escolhida       | Para no primeiro handler que resolve|
| Estado compartilhado  | Sim, via objeto de contexto      | Normalmente não                   | Normalmente não                     |
| Caso de uso típico    | Validação em várias etapas       | Escolher algoritmo (ex.: pagamento)| Middleware, tratamento de requisição|
| Aberto/Fechado        | Novas etapas facilmente          | Novas estratégias facilmente      | Novos handlers facilmente           |

> Observação: como o nosso pipeline pode parar na primeira reprovação, ele "pega emprestado" um pouco do comportamento do Chain of Responsibility. A diferença é que, quando tudo vai bem, **todas** as regras rodam e contribuem para o resultado.

---

## Melhorias aplicadas

### 1. `Order` agora é respeitado

**Antes:** o pipeline percorria as regras na ordem em que foram registradas no DI. A propriedade `Order` existia, mas ninguém olhava para ela — trocar duas linhas no `Program.cs` mudava silenciosamente a ordem da análise.

**Depois** (`Rules/EligibilityPipeline.cs`):

```csharp
foreach (var rule in rules.OrderBy(r => r.Order))
```

Agora a ordem é responsabilidade da própria regra, e a ordem de registro no DI deixa de importar.

### 2. `DisplayResult` com o `else` no lugar certo

**Antes:** o `else` que imprime a reprovação estava *dentro* do `if (result.IsApproved)`, pareado com o `if` de avisos. Resultado: quando a proposta era reprovada, **nada** era impresso; e quando era aprovada sem avisos, aparecia "REJECTED" por engano.

```csharp
// ANTES (errado)
if (result.IsApproved)
{
    // ...aprovado
    if (result.Warnings.Any()) { /* avisos */ }
    else { /* REPROVADO  <- lugar errado */ }
}
```

**Depois:**

```csharp
if (result.IsApproved)
{
    // ...aprovado
    if (result.Warnings.Any()) { /* avisos */ }
}
else
{
    // ...reprovado + motivos
}
```

### 3. `CreditScoreRule`: teste e mensagem alinhados

**Antes:** o código testava `CreditScore < 500`, mas a mensagem dizia "Mínimo exigido: 580". Um cliente com score 550 passava pela regra, mesmo a própria mensagem dizendo que ele não deveria passar.

**Depois:** o limite virou uma constante usada tanto no teste quanto na mensagem:

```csharp
private const int MinimumCreditScore = 580;

if (application.CreditScore < MinimumCreditScore)
    context.AddRejection($"... Mínimo exigido: {MinimumCreditScore}");
```

Optamos por 580 porque era o valor anunciado na mensagem e é o que o caso de teste "John Doe" (score 550, "deve ser reprovado por score baixo") esperava.

### 4. Bônus: separador do console

No `Program.cs`, a expressão `{'=' * 70}` multiplicava o código do caractere `=` (61) por 70 e imprimia o número `4270`, em vez de uma linha de `=`. Foi trocada por `new string('=', 70)`.

---

## Preparação para produção (itens 1, 5, 7 e 8)

### 1. Sem `Console.WriteLine` nas regras
O `EligibilityContext` e as regras só guardam informação. O pipeline registra tudo via `ILogger`, com logs estruturados (`{ApplicationId}`, `{RuleName}`, `{Status}`) e um *scope* com o `ApplicationId`. Nome e renda do cliente **não** vão para o log (LGPD). Quem mostra o resultado é quem chama: o `DisplayResult` do console e o front-end. Para ver os logs no console: `dotnet run --project PipelinePatternDemo -- --logs`.

### 5. Tratamento de erro
- Novo `EligibilityStatus`: `Approved`, `Rejected` e `PendingReview`.
- Se uma regra lançar exceção, o pipeline **para** e o resultado sai como `PendingReview`, com a falha em `SystemErrors`. Uma falha técnica não vira uma recusa.
- O `CancellationToken` vai da requisição HTTP até cada regra. O cancelamento sobe como `OperationCanceledException` e não é tratado como erro de regra.
- A API tem `AddProblemDetails()` + `UseExceptionHandler()`: erros inesperados viram 500 em ProblemDetails, sem stack trace.

### 7. Validação com DataAnnotations
O `LoanEvaluationRequest` tem `[Required]`, `[StringLength]` e `[Range]`, e o `Program.cs` da API chama `builder.Services.AddValidation()`. Uma requisição inválida recebe **400** (ValidationProblem) com os campos e mensagens, sem chegar ao pipeline. Como proteção extra, a regra de DTI reprova renda menor ou igual a zero em vez de dividir por zero.

### 8. Testes (`PipelinePatternDemo.Tests`)
- **Regras:** valores de fronteira de cada uma (579/580, 669/670, 36%/43%...).
- **Pipeline:** ordem pelo `Order`, curto-circuito, falha técnica levando a `PendingReview`, cancelamento.
- **API:** integração com `WebApplicationFactory<Program>`: aprovada, reprovada e 400 de validação.

Rodar: `dotnet test PipelinePatternDemo.slnx`

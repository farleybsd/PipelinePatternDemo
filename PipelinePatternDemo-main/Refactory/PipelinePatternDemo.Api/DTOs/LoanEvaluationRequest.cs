using System.ComponentModel.DataAnnotations;

namespace PipelinePatternDemo.Api.DTOs;

// O que o front-end manda para a API quando alguém clica em "Avaliar proposta".
// Mantemos este DTO separado do modelo de domínio para que mudanças na tela
// não obriguem a mexer nas regras de negócio (e vice-versa).
//
// As DataAnnotations abaixo são a "portaria" da API: dados absurdos (score 900, renda zero,
// nome vazio) são barrados com 400 Bad Request antes mesmo de chegar ao pipeline.
// Quem faz essa checagem automaticamente é o builder.Services.AddValidation() no Program.cs.
public class LoanEvaluationRequest
{
    [Required(ErrorMessage = "O nome do solicitante é obrigatório.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "O nome do solicitante deve ter entre {2} e {1} caracteres.")]
    public string ApplicantName { get; set; } = string.Empty;

    [Range(300, 850, ErrorMessage = "O score de crédito deve estar entre {1} e {2}.")]
    public int CreditScore { get; set; }

    [Range(0, 1_000_000_000, MinimumIsExclusive = true, ErrorMessage = "A renda anual deve ser maior que zero.")]
    public decimal AnnualIncome { get; set; }

    [Range(0, 100_000_000, ErrorMessage = "A dívida mensal não pode ser negativa.")]
    public decimal MonthlyDebt { get; set; }

    [Range(0, 100_000_000, MinimumIsExclusive = true, ErrorMessage = "O valor solicitado deve ser maior que zero.")]
    public decimal RequestedLoanAmount { get; set; }

    [Range(0, 60, ErrorMessage = "Os anos de emprego devem estar entre {1} e {2}.")]
    public double EmploymentYears { get; set; }

    public bool HasExistingLoans { get; set; }

    public bool IsFlaggedForFraud { get; set; }
}

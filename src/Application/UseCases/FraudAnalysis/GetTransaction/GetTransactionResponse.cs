namespace Application.UseCases.FraudAnalysis.GetTransaction;

public sealed record GetTransactionResponse(
    Guid TransactionId,
    string Status,
    MoneyDto Amount,
    DateTime ReceivedAt,
    DecisionDto? Decision,
    IReadOnlyList<HistoryEntryDto> History);

public sealed record MoneyDto(decimal Value, string Currency);

public sealed record DecisionDto(
    string Outcome,
    int Score,
    DeciderDto DecidedBy,
    DateTime DecidedAt,
    string? Justification,
    IReadOnlyList<RuleDto> Rules);

public sealed record DeciderDto(string Kind, string? ReviewerId);

public sealed record RuleDto(string Code, int Version, bool Hit, int Weight, string Reason);

public sealed record HistoryEntryDto(string Outcome, DeciderDto DecidedBy, DateTime DecidedAt);

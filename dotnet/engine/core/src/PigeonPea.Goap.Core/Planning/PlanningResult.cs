namespace PigeonPea.Goap.Planning;

/// <summary>
/// Result of a planning operation.
/// </summary>
public sealed class PlanningResult
{
    public bool Success { get; }
    public Plan? Plan { get; }
    public string? ErrorMessage { get; }

    private PlanningResult(bool success, Plan? plan, string? errorMessage)
    {
        Success = success;
        Plan = plan;
        ErrorMessage = errorMessage;
    }

    public static PlanningResult Succeeded(Plan plan) => new(true, plan, null);
    public static PlanningResult Failed(string errorMessage) => new(false, null, errorMessage);

    public override string ToString() =>
        Success ? $"Success: {Plan}" : $"Failed: {ErrorMessage}";
}

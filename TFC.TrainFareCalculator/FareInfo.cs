namespace TFC.TrainFareCalculator;

/// <summary>
/// Represents fare information for both SVC and SJT services.
/// </summary>
/// <param name="SvcFare">The fare for the stored-value card (beep card).</param>
/// <param name="SjtFare">The fare for the single journey ticket.</param>
public record FareInfo(decimal SvcFare, decimal SjtFare);
namespace Lab1.Domain.Abstractions;

/// <summary>
/// Replaceable scoring policy (OCP).
/// </summary>
public interface IMediaScoreCalculator
{
    int? Calculate(int? rating);
}

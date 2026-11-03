namespace Lab1.Domain.Scoring;

using Lab1.Domain.Abstractions;

/// <summary>
/// Maps a 1–10 rating to a score out of 100.
/// </summary>
public sealed class RatingScoreCalculator : IMediaScoreCalculator
{
    public int? Calculate(int? rating) => rating * 10;
}

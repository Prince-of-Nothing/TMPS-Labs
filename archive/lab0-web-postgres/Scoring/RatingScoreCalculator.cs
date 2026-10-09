using Lab0.Abstractions;

namespace Lab0.Scoring;

public sealed class RatingScoreCalculator : IMediaScoreCalculator
{
    public decimal? CalculateScore(int? rating) => rating * 10m;
}

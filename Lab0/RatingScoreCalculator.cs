namespace Lab0;

public sealed class RatingScoreCalculator : IMediaScoreCalculator
{
    public decimal? Calculate(int? rating) => rating / 2m;
}

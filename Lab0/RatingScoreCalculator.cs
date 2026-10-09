namespace Lab0;

public sealed class RatingScoreCalculator : IMediaScoreCalculator
{
    public int? Calculate(int? rating) => rating * 10;
}

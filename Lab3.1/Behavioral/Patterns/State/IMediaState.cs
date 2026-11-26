using Watchlist.Core.Domain;

namespace Watchlist.Behavioral.States;

public interface IMediaState
{
    MediaStatus Status { get; }
    string Name { get; }
    void Consume(MediaLifecycle lifecycle);
    void Rate(MediaLifecycle lifecycle, int rating);
    void Reopen(MediaLifecycle lifecycle);
}

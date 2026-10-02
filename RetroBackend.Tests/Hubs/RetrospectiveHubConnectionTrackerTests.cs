using RetroBackend.Hubs;
using Xunit;

namespace RetroBackend.Tests.Hubs;

public class RetrospectiveHubConnectionTrackerTests
{
    [Fact]
    public void TracksJoinAndRemovesConnectionsForUser()
    {
        var tracker = new RetrospectiveHubConnectionTracker();
        var retroA = Guid.NewGuid();
        var retroB = Guid.NewGuid();
        tracker.Add("c1", "user-1");
        tracker.SetRetrospective("c1", retroA);

        Assert.Equal(retroA, tracker.GetRetrospective("c1"));
        Assert.Equal(["c1"], tracker.GetConnectionIds("user-1"));

        tracker.SetRetrospective("c1", retroB);
        Assert.Equal(retroB, tracker.GetRetrospective("c1"));

        tracker.ClearRetrospective("c1", retroA);
        Assert.Equal(retroB, tracker.GetRetrospective("c1"));

        tracker.ClearRetrospective("c1", retroB);
        Assert.Null(tracker.GetRetrospective("c1"));

        tracker.Remove("c1");
        Assert.Empty(tracker.GetConnectionIds("user-1"));
    }

    [Fact]
    public void IsViewingRetrospective_IsTrueWhenAnyConnectionIsOnThatBoard()
    {
        var tracker = new RetrospectiveHubConnectionTracker();
        var retro = Guid.NewGuid();
        tracker.Add("away", "user-1");
        tracker.Add("here", "user-1");
        tracker.SetRetrospective("here", retro);

        Assert.True(tracker.IsViewingRetrospective("USER-1", retro));
        Assert.False(tracker.IsViewingRetrospective("user-1", Guid.NewGuid()));
        Assert.False(tracker.IsViewingRetrospective("user-2", retro));
    }
}

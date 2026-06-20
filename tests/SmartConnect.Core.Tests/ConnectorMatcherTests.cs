using SmartConnect.Core;
using Xunit;

public class ConnectorMatcherTests
{
    private static ConnectorInfo P(int i, double x, double y, double z,
        ConnectorDomain d = ConnectorDomain.Piping, double size = 0.5)
        => new(i, x, y, z, d, size);

    [Fact]
    public void Picks_closest_compatible_pair()
    {
        var a = new[] { P(0, 0, 0, 0), P(1, 10, 0, 0) };
        var b = new[] { P(0, 11, 0, 0), P(1, 0.2, 0, 0) };

        var result = ConnectorMatcher.FindBestPair(a, b);

        Assert.NotNull(result);
        Assert.Equal(0, result!.Value.A.SourceIndex);  // a[0]
        Assert.Equal(1, result.Value.B.SourceIndex);    // b[1] at distance 0.2
    }

    [Fact]
    public void Rejects_different_domains()
    {
        var a = new[] { P(0, 0, 0, 0, ConnectorDomain.Piping) };
        var b = new[] { P(0, 0, 0, 0, ConnectorDomain.Ducting) };

        Assert.Null(ConnectorMatcher.FindBestPair(a, b));
    }

    [Fact]
    public void Rejects_mismatched_pipe_size_beyond_tolerance()
    {
        var a = new[] { P(0, 0, 0, 0, size: 0.5) };
        var b = new[] { P(0, 0, 0, 0, size: 0.9) };

        Assert.Null(ConnectorMatcher.FindBestPair(a, b, sizeTolerance: 0.01));
    }

    [Fact]
    public void Returns_null_when_no_connectors()
    {
        Assert.Null(ConnectorMatcher.FindBestPair(
            System.Array.Empty<ConnectorInfo>(),
            new[] { P(0, 0, 0, 0) }));
    }
}

namespace FastConnect.Core;

public static class ConnectorMatcher
{
    /// <summary>
    /// Returns the spatially closest, compatible pair of connectors (one from setA,
    /// one from setB), or null when no pair is compatible.
    /// </summary>
    public static (ConnectorInfo A, ConnectorInfo B)? FindBestPair(
        IReadOnlyList<ConnectorInfo> setA,
        IReadOnlyList<ConnectorInfo> setB,
        double sizeTolerance = 1e-9)
    {
        (ConnectorInfo, ConnectorInfo)? best = null;
        double min = double.MaxValue;

        foreach (var a in setA)
        foreach (var b in setB)
        {
            if (!a.CompatibleWith(b, sizeTolerance)) continue;
            double d = a.DistanceTo(b);
            if (d < min) { min = d; best = (a, b); }
        }
        return best;
    }
}

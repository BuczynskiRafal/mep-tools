namespace SmartConnect.Core;

public enum ConnectorDomain
{
    Undefined,
    Piping,
    Ducting,
    Electrical,
    CableTray
}

/// <summary>
/// Pure, testable description of a single connector. No Revit types.
/// SourceIndex lets the Revit layer recover the live Connector afterwards.
/// </summary>
public readonly record struct ConnectorInfo(
    int SourceIndex,
    double X,
    double Y,
    double Z,
    ConnectorDomain Domain,
    double Size)        // diameter for round connectors; 0 when irrelevant/unknown
{
    public double DistanceTo(in ConnectorInfo other)
    {
        double dx = X - other.X, dy = Y - other.Y, dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public bool CompatibleWith(in ConnectorInfo other, double sizeTolerance)
    {
        if (Domain != other.Domain) return false;
        if (Domain == ConnectorDomain.Piping && Size > 0 && other.Size > 0
            && Math.Abs(Size - other.Size) > sizeTolerance)
            return false;
        return true;
    }
}

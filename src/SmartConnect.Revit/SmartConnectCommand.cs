using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SmartConnect.Core;

namespace SmartConnect.Revit;

[Transaction(TransactionMode.Manual)]
public class SmartConnectCommand : IExternalCommand
{
    private const double SizeToleranceFeet = 1.0 / 304.8; // ~1 mm

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        Document doc = uidoc.Document;

        Element fixedEl, movingEl;
        try
        {
            var filter = new MepConnectableFilter();
            Reference r1 = uidoc.Selection.PickObject(ObjectType.Element, filter, "Pick the first element (stays in place).");
            Reference r2 = uidoc.Selection.PickObject(ObjectType.Element, filter, "Pick the second element (will be moved).");
            fixedEl  = doc.GetElement(r1);
            movingEl = doc.GetElement(r2);
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return Result.Cancelled;
        }

        List<Connector> freeFixed  = GetFreeConnectors(fixedEl);
        List<Connector> freeMoving = GetFreeConnectors(movingEl);

        if (freeFixed.Count == 0 || freeMoving.Count == 0)
        {
            TaskDialog.Show("Smart Connect", "At least one element has no free connectors.");
            return Result.Cancelled;
        }

        var infosFixed  = freeFixed.Select((c, i) => ToInfo(c, i)).ToList();
        var infosMoving = freeMoving.Select((c, i) => ToInfo(c, i)).ToList();

        var match = ConnectorMatcher.FindBestPair(infosFixed, infosMoving, SizeToleranceFeet);
        if (match is null)
        {
            TaskDialog.Show("Smart Connect", "No compatible connector pair found (different domain or size).");
            return Result.Cancelled;
        }

        Connector tgt = freeFixed[match.Value.A.SourceIndex];   // fixed
        Connector src = freeMoving[match.Value.B.SourceIndex];  // moving

        using var t = new Transaction(doc, "Smart Connect MEP");
        t.Start();
        try
        {
            AlignConnectors(doc, movingEl.Id, src, tgt);
            src.ConnectTo(tgt);
            t.Commit();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            t.RollBack();
            message = ex.Message;
            TaskDialog.Show("Smart Connect — error", ex.Message);
            return Result.Failed;
        }
    }

    private static List<Connector> GetFreeConnectors(Element element)
    {
        var result = new List<Connector>();
        ConnectorManager? cm = element switch
        {
            MEPCurve curve => curve.ConnectorManager,
            FamilyInstance fi when fi.MEPModel != null => fi.MEPModel.ConnectorManager,
            _ => null
        };
        if (cm == null) return result;

        foreach (Connector c in cm.Connectors)
            if (c.ConnectorType == ConnectorType.End && !c.IsConnected)
                result.Add(c);
        return result;
    }

    private static ConnectorInfo ToInfo(Connector c, int index)
    {
        XYZ o = c.Origin;
        double size = 0;
        try { size = c.Radius * 2.0; } catch { /* non-round connector */ }

        ConnectorDomain domain = c.Domain switch
        {
            Domain.DomainPiping            => ConnectorDomain.Piping,
            Domain.DomainHvac              => ConnectorDomain.Ducting,
            Domain.DomainElectrical        => ConnectorDomain.Electrical,
            Domain.DomainCableTrayConduit  => ConnectorDomain.CableTray,
            _                              => ConnectorDomain.Undefined
        };
        return new ConnectorInfo(index, o.X, o.Y, o.Z, domain, size);
    }

    /// <summary>
    /// Rotates and moves the moving element so its 'source' connector coincides with the
    /// 'target' connector and faces it head-on (the connector axes point at each other).
    /// </summary>
    private static void AlignConnectors(Document doc, ElementId movingId, Connector source, Connector target)
    {
        XYZ sourceDir  = source.CoordinateSystem.BasisZ;        // outward direction of the connector
        XYZ desiredDir = target.CoordinateSystem.BasisZ.Negate();

        double angle = sourceDir.AngleTo(desiredDir);
        if (angle > 1e-9)
        {
            XYZ axis = angle < Math.PI - 1e-9
                ? sourceDir.CrossProduct(desiredDir).Normalize()
                : (Math.Abs(sourceDir.Z) < 0.9
                    ? sourceDir.CrossProduct(XYZ.BasisZ).Normalize()   // anti-parallel case
                    : sourceDir.CrossProduct(XYZ.BasisX).Normalize());

            // The axis passes through the connector Origin, so the connector point does not move during rotation.
            Line rotAxis = Line.CreateUnbound(source.Origin, axis);
            ElementTransformUtils.RotateElement(doc, movingId, rotAxis, angle);
        }

        // The Connector is "live": after rotation its Origin is up to date. Now move it into place.
        XYZ translation = target.Origin - source.Origin;
        if (!translation.IsZeroLength())
            ElementTransformUtils.MoveElement(doc, movingId, translation);
    }
}

public class MepConnectableFilter : ISelectionFilter
{
    public bool AllowElement(Element e)
        => e is MEPCurve || (e is FamilyInstance fi && fi.MEPModel != null);

    public bool AllowReference(Reference reference, XYZ position) => false;
}

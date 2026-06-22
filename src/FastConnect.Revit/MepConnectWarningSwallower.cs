using Autodesk.Revit.DB;

namespace FastConnect.Revit;

// Clears warnings raised while connecting MEP elements (e.g. "moving an element connected to
// the system") so the user isn't interrupted. Errors are left untouched to still roll back.
public class MepConnectWarningSwallower : IFailuresPreprocessor
{
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        foreach (FailureMessageAccessor failure in failuresAccessor.GetFailureMessages())
        {
            if (failure.GetSeverity() == FailureSeverity.Warning)
                failuresAccessor.DeleteWarning(failure);
        }

        return FailureProcessingResult.Continue;
    }
}

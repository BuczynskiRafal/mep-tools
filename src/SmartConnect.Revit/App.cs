using System.Reflection;
using Autodesk.Revit.UI;

namespace SmartConnect.Revit;

public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication app)
    {
        const string tabName = "MEP Tools";
        try { app.CreateRibbonTab(tabName); } catch { /* tab already exists */ }

        RibbonPanel panel = app.CreateRibbonPanel(tabName, "Connections");
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        var button = new PushButtonData(
            "SmartConnectBtn",
            "Smart\nConnect",
            assemblyPath,
            "SmartConnect.Revit.SmartConnectCommand")
        {
            ToolTip = "Connects two picked MEP elements: aligns their geometry and creates the connection."
            // LargeImage = ...  // optional 32x32 icon (see 'what's next' step)
        };

        panel.AddItem(button);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
}

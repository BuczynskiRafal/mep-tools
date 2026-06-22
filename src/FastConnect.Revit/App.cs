using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace FastConnect.Revit;

public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication app)
    {
        const string tabName = "MEP Tools";
        try { app.CreateRibbonTab(tabName); } catch { /* tab already exists */ }

        RibbonPanel panel = GetOrCreateRibbonPanel(app, tabName, "Connections");
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        var button = new PushButtonData(
            "FastConnectBtn",
            "Fast\nConnect",
            assemblyPath,
            "FastConnect.Revit.FastConnectCommand")
        {
            ToolTip = "Connects two picked MEP elements: aligns their geometry and creates the connection.",
            LargeImage = LoadPng("FastConnect.Revit.Resources.icon32.png"),
            Image      = LoadPng("FastConnect.Revit.Resources.icon16.png"),
        };

        panel.AddItem(button);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;

    private static RibbonPanel GetOrCreateRibbonPanel(UIControlledApplication app, string tabName, string panelName)
    {
        foreach (RibbonPanel panel in app.GetRibbonPanels(tabName))
        {
            if (panel.Name == panelName) return panel;
        }

        return app.CreateRibbonPanel(tabName, panelName);
    }

    /// <summary>Loads a PNG embedded in the assembly into an ImageSource for ribbon buttons.</summary>
    private static ImageSource? LoadPng(string resourceName)
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null) return null;
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return decoder.Frames[0];
    }
}

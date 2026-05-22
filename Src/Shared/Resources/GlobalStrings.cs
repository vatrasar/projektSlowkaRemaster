using System.Resources;

namespace ProjektSlowkaRemasterd.Src.Shared.Resources;

/// <summary>
/// Provides strongly-typed access to global localized strings.
/// </summary>
public static class GlobalStrings
{
    private static readonly ResourceManager ResourceManager = 
        new ResourceManager("ProjektSlowkaRemasterd.Src.Shared.Resources.GlobalStrings", typeof(GlobalStrings).Assembly);

    public static string SaveButton => ResourceManager.GetString("SaveButton") ?? "Save";
    public static string CancelButton => ResourceManager.GetString("CancelButton") ?? "Cancel";
    public static string AppTitle => ResourceManager.GetString("AppTitle") ?? "Spaced Repetition Flashcards";
}

using System.Resources;

namespace ProjektSlowkaRemasterd.Src.Features.Shell.Resources;

/// <summary>
/// Provides strongly-typed access to localized strings for the Shell feature.
/// </summary>
public static class ShellStrings
{
    private static readonly ResourceManager ResourceManager = 
        new ResourceManager("ProjektSlowkaRemasterd.Src.Features.Shell.Resources.ShellStrings", typeof(ShellStrings).Assembly);

    public static string AppLogoText => ResourceManager.GetString("AppLogoText") ?? "Slowka Remaster";
    public static string NavHome => ResourceManager.GetString("NavHome") ?? "Home";
    public static string NavReview => ResourceManager.GetString("NavReview") ?? "Review for Today";
    public static string NavTraining => ResourceManager.GetString("NavTraining") ?? "Training Mode";
    public static string NavManage => ResourceManager.GetString("NavManage") ?? "Manage";
    public static string NavSearch => ResourceManager.GetString("NavSearch") ?? "Search";
    public static string NavSettings => ResourceManager.GetString("NavSettings") ?? "Settings";
}

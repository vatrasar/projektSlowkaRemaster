using System.Resources;

namespace ProjektSlowkaRemasterd.Src.Features.Search.Resources;

/// <summary>
/// Provides strongly-typed access to localized strings for the Search feature.
/// </summary>
public static class SearchStrings
{
    private static readonly ResourceManager ResourceManager = 
        new ResourceManager("ProjektSlowkaRemasterd.Src.Features.Search.Resources.SearchStrings", typeof(SearchStrings).Assembly);

    public static string SearchTitle => ResourceManager.GetString("SearchTitle") ?? "Search Flashcards";
    public static string SearchPlaceholder => ResourceManager.GetString("SearchPlaceholder") ?? "Enter keyword to search in questions & answers...";
    public static string CategoryLabel => ResourceManager.GetString("CategoryLabel") ?? "Category:";
    public static string TopicLabel => ResourceManager.GetString("TopicLabel") ?? "Topic:";
    public static string SectionLabel => ResourceManager.GetString("SectionLabel") ?? "Section:";
    public static string StatusLabel => ResourceManager.GetString("StatusLabel") ?? "Status:";
    public static string EditButton => ResourceManager.GetString("EditButton") ?? "Edit";
    public static string DeleteButton => ResourceManager.GetString("DeleteButton") ?? "Delete";
    public static string NoResultsText => ResourceManager.GetString("NoResultsText") ?? "No questions match the search criteria.";
    public static string CategoryHeader => ResourceManager.GetString("CategoryHeader") ?? "Category";
    public static string TopicHeader => ResourceManager.GetString("TopicHeader") ?? "Topic";
    public static string SectionHeader => ResourceManager.GetString("SectionHeader") ?? "Section";
    public static string StatusHeader => ResourceManager.GetString("StatusHeader") ?? "Status";
}

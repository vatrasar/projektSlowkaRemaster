using System.Resources;

namespace ProjektSlowkaRemasterd.Src.Features.Question.Resources;

/// <summary>
/// Provides strongly-typed access to localized strings for the Question feature.
/// </summary>
public static class QuestionStrings
{
    private static readonly ResourceManager ResourceManager = 
        new ResourceManager("ProjektSlowkaRemasterd.Src.Features.Question.Resources.QuestionStrings", typeof(QuestionStrings).Assembly);

    public static string BulkImportTitle => ResourceManager.GetString("BulkImportTitle") ?? "Bulk Import Questions";
    public static string SelectFileButton => ResourceManager.GetString("SelectFileButton") ?? "Choose File";
    public static string ImportButton => ResourceManager.GetString("ImportButton") ?? "Import Questions";
    public static string SuccessMessage => ResourceManager.GetString("SuccessMessage") ?? "Successfully imported {0} questions!";
    public static string NoFileChosen => ResourceManager.GetString("NoFileChosen") ?? "No file chosen";
    public static string PastePrompt => ResourceManager.GetString("PastePrompt") ?? "Or paste the file contents directly below:";
    public static string CategoryLabel => ResourceManager.GetString("CategoryLabel") ?? "Category";
    public static string TopicLabel => ResourceManager.GetString("TopicLabel") ?? "Topic (Optional)";
    public static string SectionLabel => ResourceManager.GetString("SectionLabel") ?? "Section (Optional)";
    public static string FileLabel => ResourceManager.GetString("FileLabel") ?? "Select File";
    public static string BackButtonText => ResourceManager.GetString("BackButtonText") ?? "← Back";
}

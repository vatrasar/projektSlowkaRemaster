namespace ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.AddCategory;

public record AddCategoryState
{
    public string CategoryName { get; init; } = string.Empty;
    public bool IsDoubleSided { get; init; } = false;
    public string ErrorMessage { get; init; } = string.Empty;
    public bool IsLoading { get; init; } = false;
}

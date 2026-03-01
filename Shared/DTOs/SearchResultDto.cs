namespace MMGC.Shared.DTOs;

/// <summary>
/// Data transfer object for unified search results.
/// </summary>
public class SearchResultDto
{
    /// <summary>
    /// Type of entity found (Doctor, Patient, Procedure, etc.).
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier of the entity.
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Primary display name.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Secondary display information (specialization, MR number, etc.).
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Additional descriptive text.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL to view details of this entity.
    /// </summary>
    public string DetailUrl { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score for sorting results (0-100).
    /// </summary>
    public int RelevanceScore { get; set; }
}

/// <summary>
/// Grouped search results container.
/// </summary>
public class GroupedSearchResultDto
{
    /// <summary>
    /// Category name (Doctors, Patients, Procedures, etc.).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Results in this category.
    /// </summary>
    public List<SearchResultDto> Results { get; set; } = new();

    /// <summary>
    /// Total count of results in this category.
    /// </summary>
    public int TotalCount { get; set; }
}

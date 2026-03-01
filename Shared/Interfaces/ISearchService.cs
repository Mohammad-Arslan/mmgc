using MMGC.Shared.DTOs;

namespace MMGC.Shared.Interfaces;

/// <summary>
/// Service contract for unified search across doctors, patients, and procedures.
/// Implements extensible search with dynamic filtering.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Performs unified search across all entities.
    /// </summary>
    /// <param name="query">Search query string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Grouped search results by entity type.</returns>
    Task<List<GroupedSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for doctors by name, specialization, or availability.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="specialization">Optional specialization filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of doctor search results.</returns>
    Task<List<SearchResultDto>> SearchDoctorsAsync(string query, string? specialization = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for patients by MR number or name.
    /// Requires staff/admin authorization.
    /// </summary>
    Task<List<SearchResultDto>> SearchPatientsAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for procedures by type or name.
    /// </summary>
    Task<List<SearchResultDto>> SearchProceduresAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for lab test categories.
    /// </summary>
    Task<List<SearchResultDto>> SearchLabTestsAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Advanced search with multiple filters.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="filters">Dictionary of filter key-value pairs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered and ranked results.</returns>
    Task<List<SearchResultDto>> AdvancedSearchAsync(string query, Dictionary<string, string>? filters = null, CancellationToken cancellationToken = default);
}

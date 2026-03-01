using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMGC.Shared.DTOs;
using MMGC.Shared.Interfaces;

namespace MMGC.Pages.Search;

/// <summary>
/// PageModel for search results page.
/// Displays grouped search results for doctors, procedures, and other entities.
/// </summary>
[Authorize]
public class ResultsModel : PageModel
{
    private readonly ISearchService _searchService;
    private readonly ILogger<ResultsModel> _logger;

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Specialization { get; set; }

    public List<GroupedSearchResultDto> Results { get; set; } = new();
    public List<SearchResultDto> FilteredResults { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool HasSearched { get; set; } = false;
    public int TotalResults { get; set; } = 0;

    public ResultsModel(ISearchService searchService, ILogger<ResultsModel> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Query))
            {
                return Page();
            }

            HasSearched = true;

            // Filter results by entity type if specified
            if (!string.IsNullOrEmpty(EntityType))
            {
                FilteredResults = EntityType.ToLower() switch
                {
                    "doctor" => await _searchService.SearchDoctorsAsync(Query, Specialization),
                    "patient" => await _searchService.SearchPatientsAsync(Query),
                    "procedure" => await _searchService.SearchProceduresAsync(Query),
                    _ => new List<SearchResultDto>()
                };
                TotalResults = FilteredResults.Count;
            }
            else
            {
                // Get all grouped results
                Results = await _searchService.SearchAsync(Query);
                TotalResults = Results.Sum(g => g.TotalCount);
            }

            if (TotalResults == 0)
            {
                ErrorMessage = $"No results found for '{Query}'";
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for query: {Query}", Query);
            ErrorMessage = "An error occurred while searching. Please try again.";
            HasSearched = true;
            return Page();
        }
    }

    public async Task<IActionResult> OnGetAdvancedSearchAsync(Dictionary<string, string>? filters = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Query))
            {
                return new JsonResult(new { error = "Query is required" })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            FilteredResults = await _searchService.AdvancedSearchAsync(Query, filters);
            return new JsonResult(new { items = FilteredResults, count = FilteredResults.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced search");
            return new JsonResult(new { error = "Search failed" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MMGC.Data;
using MMGC.Shared.Constants;
using MMGC.Shared.DTOs;
using MMGC.Shared.Interfaces;

namespace MMGC.Features.Search.Services;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SearchService> _logger;

    public SearchService(ApplicationDbContext context, ILogger<SearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GroupedSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < SystemConstants.MIN_SEARCH_CHARACTERS)
            return new List<GroupedSearchResultDto>();

        var results = new List<GroupedSearchResultDto>();
        var doctorResults = await SearchDoctorsAsync(query, cancellationToken: cancellationToken);
        if (doctorResults.Count > 0)
            results.Add(new GroupedSearchResultDto { Category = "Doctors", Results = doctorResults.Take(SystemConstants.MAX_SEARCH_RESULTS_PER_CATEGORY).ToList(), TotalCount = doctorResults.Count });

        var procedureResults = await SearchProceduresAsync(query, cancellationToken: cancellationToken);
        if (procedureResults.Count > 0)
            results.Add(new GroupedSearchResultDto { Category = "Procedures", Results = procedureResults.Take(SystemConstants.MAX_SEARCH_RESULTS_PER_CATEGORY).ToList(), TotalCount = procedureResults.Count });

        return results;
    }

    public async Task<List<SearchResultDto>> SearchDoctorsAsync(string query, string? specialization = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < SystemConstants.MIN_SEARCH_CHARACTERS)
            return new List<SearchResultDto>();

        var searchQuery = query.ToLower();
        var doctors = await _context.Doctors
            .AsNoTracking()
            .Where(d => (EF.Functions.Like(d.FirstName, $"%{searchQuery}%") || EF.Functions.Like(d.LastName, $"%{searchQuery}%") || EF.Functions.Like(d.Specialization, $"%{searchQuery}%")) &&
                (string.IsNullOrEmpty(specialization) || d.Specialization == specialization))
            .Select(d => new SearchResultDto
            {
                EntityType = "Doctor",
                EntityId = d.Id,
                Title = d.FullName,
                Subtitle = d.Specialization,
                Description = $"License: {d.LicenseNumber}",
                DetailUrl = $"/Doctors/Details/{d.Id}",
                RelevanceScore = CalculateDoctorRelevance(d.FirstName, d.LastName, d.Specialization, searchQuery)
            })
            .OrderByDescending(r => r.RelevanceScore)
            .Take(SystemConstants.MAX_SEARCH_RESULTS_PER_CATEGORY)
            .ToListAsync(cancellationToken);

        return doctors;
    }

    public async Task<List<SearchResultDto>> SearchPatientsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < SystemConstants.MIN_SEARCH_CHARACTERS)
            return new List<SearchResultDto>();

        var searchQuery = query.ToLower();
        var patients = await _context.Patients
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.MRNumber, $"%{searchQuery}%") || EF.Functions.Like(p.FirstName, $"%{searchQuery}%") || EF.Functions.Like(p.LastName, $"%{searchQuery}%"))
            .Select(p => new SearchResultDto
            {
                EntityType = "Patient",
                EntityId = p.Id,
                Title = p.FullName,
                Subtitle = $"MR: {p.MRNumber}",
                Description = $"DOB: {p.DateOfBirth:MMMM dd, yyyy}",
                DetailUrl = $"/Patients/Details/{p.Id}",
                RelevanceScore = CalculatePatientRelevance(p.MRNumber, p.FirstName, p.LastName, searchQuery)
            })
            .OrderByDescending(r => r.RelevanceScore)
            .Take(SystemConstants.MAX_SEARCH_RESULTS_PER_CATEGORY)
            .ToListAsync(cancellationToken);

        return patients;
    }

    public async Task<List<SearchResultDto>> SearchProceduresAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < SystemConstants.MIN_SEARCH_CHARACTERS)
            return new List<SearchResultDto>();

        var searchQuery = query.ToLower();
        var procedures = await _context.Procedures
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.ProcedureName, $"%{searchQuery}%") || EF.Functions.Like(p.ProcedureType, $"%{searchQuery}%"))
            .Include(p => p.Patient)
            .Select(p => new SearchResultDto
            {
                EntityType = "Procedure",
                EntityId = p.Id,
                Title = p.ProcedureName,
                Subtitle = p.Patient!.FullName,
                Description = p.ProcedureType,
                DetailUrl = $"/Procedures/Details/{p.Id}",
                RelevanceScore = CalculateProcedureRelevance(p.ProcedureName, p.ProcedureType, searchQuery)
            })
            .OrderByDescending(r => r.RelevanceScore)
            .Take(SystemConstants.MAX_SEARCH_RESULTS_PER_CATEGORY)
            .ToListAsync(cancellationToken);

        return procedures;
    }

    public async Task<List<SearchResultDto>> SearchLabTestsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < SystemConstants.MIN_SEARCH_CHARACTERS)
            return new List<SearchResultDto>();

        var searchQuery = query.ToLower();
        var labTests = await _context.LabTests
            .AsNoTracking()
            .Where(lt => EF.Functions.Like(lt.TestName, $"%{searchQuery}%"))
            .Include(lt => lt.LabTestCategory)
            .Select(lt => new SearchResultDto
            {
                EntityType = "LabTest",
                EntityId = lt.Id,
                Title = lt.TestName,
                Subtitle = lt.LabTestCategory!.CategoryName,
                Description = $"Status: {lt.Status}",
                DetailUrl = $"/LabTests/Details/{lt.Id}",
                RelevanceScore = CalculateLabTestRelevance(lt.TestName, lt.LabTestCategory!.CategoryName, searchQuery)
            })
            .OrderByDescending(r => r.RelevanceScore)
            .Take(SystemConstants.MAX_SEARCH_RESULTS_PER_CATEGORY)
            .ToListAsync(cancellationToken);

        return labTests;
    }

    public async Task<List<SearchResultDto>> AdvancedSearchAsync(string query, Dictionary<string, string>? filters = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < SystemConstants.MIN_SEARCH_CHARACTERS)
            return new List<SearchResultDto>();

        filters ??= new Dictionary<string, string>();
        if (filters.ContainsKey("entityType"))
        {
            var entityType = filters["entityType"].ToLower();
            return entityType switch
            {
                "doctor" => await SearchDoctorsAsync(query, filters.GetValueOrDefault("specialization"), cancellationToken),
                "patient" => await SearchPatientsAsync(query, cancellationToken),
                "procedure" => await SearchProceduresAsync(query, cancellationToken),
                "labtest" => await SearchLabTestsAsync(query, cancellationToken),
                _ => new List<SearchResultDto>()
            };
        }

        var allResults = await SearchAsync(query, cancellationToken);
        return allResults.SelectMany(g => g.Results).ToList();
    }

    private static int CalculateDoctorRelevance(string firstName, string lastName, string specialization, string query)
    {
        int score = 0;
        if (firstName.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (lastName.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (specialization.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 80;
        if (firstName.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 50;
        if (lastName.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 50;
        if (specialization.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 40;
        if (firstName.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 20;
        if (lastName.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 20;
        if (specialization.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 15;
        return score;
    }

    private static int CalculatePatientRelevance(string mrNumber, string firstName, string lastName, string query)
    {
        int score = 0;
        if (mrNumber.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 150;
        if (mrNumber.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (firstName.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 80;
        if (lastName.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 80;
        if (firstName.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 40;
        if (lastName.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 40;
        if (firstName.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 15;
        if (lastName.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 15;
        return score;
    }

    private static int CalculateProcedureRelevance(string procedureName, string procedureType, string query)
    {
        int score = 0;
        if (procedureName.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (procedureName.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 50;
        if (procedureName.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 25;
        if (procedureType.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 15;
        return score;
    }

    private static int CalculateLabTestRelevance(string testName, string categoryName, string query)
    {
        int score = 0;
        if (testName.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (testName.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 50;
        if (testName.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 25;
        if (categoryName.Equals(query, StringComparison.OrdinalIgnoreCase)) score += 80;
        if (categoryName.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 20;
        return score;
    }
}

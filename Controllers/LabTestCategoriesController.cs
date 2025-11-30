using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMGC.Models;
using MMGC.Services;
using MMGC.Repositories;

namespace MMGC.Controllers;

[Authorize]
public class LabTestCategoriesController : Controller
{
    private readonly ILabTestService _labTestService;
    private readonly IRepository<LabTestCategory> _categoryRepository;

    public LabTestCategoriesController(
        ILabTestService labTestService,
        IRepository<LabTestCategory> categoryRepository)
    {
        _labTestService = labTestService;
        _categoryRepository = categoryRepository;
    }

    // GET: LabTestCategories
    public async Task<IActionResult> Index()
    {
        var categories = await _labTestService.GetAllCategoriesAsync();
        return View(categories);
    }

    // GET: LabTestCategories/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: LabTestCategories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CategoryName,Description,IsActive")] LabTestCategory category)
    {
        if (ModelState.IsValid)
        {
            await _categoryRepository.AddAsync(category);
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // GET: LabTestCategories/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _categoryRepository.GetByIdAsync(id.Value);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    // POST: LabTestCategories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,CategoryName,Description,IsActive")] LabTestCategory category)
    {
        if (id != category.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _categoryRepository.UpdateAsync(category);
            }
            catch
            {
                if (!await CategoryExists(category.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    private async Task<bool> CategoryExists(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        return category != null;
    }
}

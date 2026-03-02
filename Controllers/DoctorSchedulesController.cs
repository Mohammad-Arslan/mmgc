using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Models;
using MMGC.Services;
using MMGC.Shared.Constants;

namespace MMGC.Controllers;

/// <summary>
/// Manages doctor time slots. Admin can manage any doctor; Doctor can manage own slots only.
/// </summary>
[Authorize(Roles = "Admin,Doctor")]
public class DoctorSchedulesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDoctorService _doctorService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DoctorSchedulesController> _logger;

    public DoctorSchedulesController(
        ApplicationDbContext context,
        IDoctorService doctorService,
        UserManager<ApplicationUser> userManager,
        ILogger<DoctorSchedulesController> logger)
    {
        _context = context;
        _doctorService = doctorService;
        _userManager = userManager;
        _logger = logger;
    }

    private async Task<Doctor?> GetCurrentDoctorAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;
        var doctor = await _doctorService.GetDoctorByUserIdAsync(user.Id);
        if (doctor == null && !string.IsNullOrEmpty(user.Email))
        {
            var all = await _doctorService.GetAllDoctorsAsync();
            doctor = all.FirstOrDefault(d => d.Email?.ToLower() == user.Email.ToLower());
        }
        return doctor;
    }

    /// <summary>
    /// Admin: pass doctorId to filter. Doctor: sees only own schedules.
    /// </summary>
    public async Task<IActionResult> Index(int? doctorId)
    {
        int? effectiveDoctorId = null;
        if (User.IsInRole("Admin"))
        {
            effectiveDoctorId = doctorId;
        }
        else
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
            {
                ViewBag.ErrorMessage = "Doctor profile not found. Please contact administrator.";
                return View(new List<DoctorSchedule>());
            }
            effectiveDoctorId = doctor.Id;
        }

        var query = _context.DoctorSchedules
            .Include(ds => ds.Doctor)
            .AsNoTracking()
            .Where(ds => ds.ScheduleDate >= DateTime.Today);

        if (effectiveDoctorId.HasValue)
            query = query.Where(ds => ds.DoctorId == effectiveDoctorId.Value);

        var schedules = await query.OrderBy(ds => ds.ScheduleDate).ThenBy(ds => ds.StartTime).ToListAsync();

        if (User.IsInRole("Admin"))
        {
            ViewBag.Doctors = new SelectList(
                await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FirstName).ToListAsync(),
                "Id", "FullName", doctorId);
            ViewBag.SelectedDoctorId = doctorId;
        }

        return View(schedules);
    }

    public async Task<IActionResult> Create(int? doctorId)
    {
        await PopulateDoctorsDropdownAsync(doctorId);
        var model = new DoctorSchedule
        {
            ScheduleDate = DateTime.Today,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsAvailable = true,
            DayOfWeek = DateTime.Today.ToString("dddd")
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DoctorSchedule schedule)
    {
        // Clear navigation property - not bound from form (same as AppointmentsController)
        ModelState.Remove(nameof(DoctorSchedule.Doctor));

        // Fallback: HTML5 time input sends "HH:mm" which may not bind to TimeSpan in some setups
        if (Request.Form.TryGetValue("StartTime", out var startVal) && Request.Form.TryGetValue("EndTime", out var endVal))
        {
            if (TimeSpan.TryParse(startVal.ToString(), out var startParsed))
                schedule.StartTime = startParsed;
            if (TimeSpan.TryParse(endVal.ToString(), out var endParsed))
                schedule.EndTime = endParsed;
        }

        // DayOfWeek is derived from ScheduleDate - set before validation
        schedule.DayOfWeek = schedule.ScheduleDate.ToString("dddd");
        ModelState.Remove(nameof(schedule.DayOfWeek));

        if (User.IsInRole("Doctor"))
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
            {
                ViewBag.ErrorMessage = "Doctor profile not found.";
                await PopulateDoctorsDropdownAsync(null);
                return View(schedule);
            }
            schedule.DoctorId = doctor.Id;
            ModelState.Remove(nameof(schedule.DoctorId));
        }

        if (schedule.EndTime <= schedule.StartTime)
        {
            ModelState.AddModelError(nameof(schedule.EndTime), "End time must be after start time.");
        }

        if (schedule.ScheduleDate < DateTime.Today)
        {
            ModelState.AddModelError(nameof(schedule.ScheduleDate), "Schedule date cannot be in the past.");
        }

        if (ModelState.IsValid)
        {
            schedule.DayOfWeek = schedule.ScheduleDate.ToString("dddd");
            schedule.CreatedDate = DateTime.Now;
            schedule.Doctor = null; // EF uses FK only
            _context.Add(schedule);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Doctor schedule created: DoctorId={DoctorId}, Date={Date}", schedule.DoctorId, schedule.ScheduleDate);
            TempData["SuccessMessage"] = "Time slot added successfully.";
            return RedirectToAction(nameof(Index), new { doctorId = User.IsInRole("Admin") ? schedule.DoctorId : (int?)null });
        }

        await PopulateDoctorsDropdownAsync(schedule.DoctorId ?? 0);
        return View(schedule);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var schedule = await _context.DoctorSchedules.FindAsync(id);
        if (schedule == null) return NotFound();

        if (User.IsInRole("Doctor"))
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null || schedule.DoctorId != doctor.Id)
                return Forbid();
        }

        await PopulateDoctorsDropdownAsync(schedule.DoctorId);
        return View(schedule);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DoctorSchedule schedule)
    {
        if (id != schedule.Id) return NotFound();

        ModelState.Remove(nameof(DoctorSchedule.Doctor));
        schedule.DayOfWeek = schedule.ScheduleDate.ToString("dddd");
        ModelState.Remove(nameof(schedule.DayOfWeek));

        if (User.IsInRole("Doctor"))
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null || schedule.DoctorId != doctor.Id)
                return Forbid();
        }

        if (schedule.EndTime <= schedule.StartTime)
            ModelState.AddModelError(nameof(schedule.EndTime), "End time must be after start time.");

        if (schedule.ScheduleDate < DateTime.Today)
            ModelState.AddModelError(nameof(schedule.ScheduleDate), "Schedule date cannot be in the past.");

        if (ModelState.IsValid)
        {
            schedule.DayOfWeek = schedule.ScheduleDate.ToString("dddd");
            schedule.Doctor = null; // EF uses FK only
            try
            {
                _context.Update(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Time slot updated successfully.";
                return RedirectToAction(nameof(Index), new { doctorId = User.IsInRole("Admin") ? schedule.DoctorId : (int?)null });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.DoctorSchedules.AnyAsync(ds => ds.Id == id))
                    return NotFound();
                throw;
            }
        }

        await PopulateDoctorsDropdownAsync(schedule.DoctorId);
        return View(schedule);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var schedule = await _context.DoctorSchedules.Include(ds => ds.Doctor).FirstOrDefaultAsync(ds => ds.Id == id);
        if (schedule == null) return NotFound();

        if (User.IsInRole("Doctor"))
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null || schedule.DoctorId != doctor.Id)
                return Forbid();
        }

        return View(schedule);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var schedule = await _context.DoctorSchedules.FindAsync(id);
        if (schedule == null) return NotFound();

        if (User.IsInRole("Doctor"))
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null || schedule.DoctorId != doctor.Id)
                return Forbid();
        }

        _context.DoctorSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Time slot removed.";
        return RedirectToAction(nameof(Index), new { doctorId = User.IsInRole("Admin") ? schedule.DoctorId : (int?)null });
    }

    private async Task PopulateDoctorsDropdownAsync(int? selectedId)
    {
        if (User.IsInRole("Admin"))
        {
            var doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FirstName).ToListAsync();
            ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", selectedId);
        }
        else
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor != null)
                ViewBag.Doctors = new SelectList(new List<Doctor> { doctor }, "Id", "FullName", doctor.Id);
        }
    }
}

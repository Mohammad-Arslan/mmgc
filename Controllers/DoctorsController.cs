using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMGC.Models;
using MMGC.Services;

namespace MMGC.Controllers;

[Authorize]
public class DoctorsController : Controller
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    // GET: Doctors
    public async Task<IActionResult> Index()
    {
        var doctors = await _doctorService.GetAllDoctorsAsync();
        var doctorsWithStats = new List<object>();
        
        foreach (var doctor in doctors)
        {
            var revenue = await _doctorService.GetDoctorTotalRevenueAsync(doctor.Id);
            var appointments = await _doctorService.GetDoctorAppointmentCountAsync(doctor.Id);
            
            doctorsWithStats.Add(new
            {
                doctor.Id,
                doctor.FirstName,
                doctor.LastName,
                doctor.FullName,
                doctor.Specialization,
                doctor.ContactNumber,
                doctor.Email,
                doctor.IsActive,
                TotalAppointments = appointments,
                TotalRevenue = revenue
            });
        }
        
        ViewBag.DoctorsWithStats = doctorsWithStats;
        return View(doctors);
    }

    // GET: Doctors/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var doctor = await _doctorService.GetDoctorWithDetailsAsync(id.Value);
        if (doctor == null)
        {
            return NotFound();
        }

        ViewBag.TotalRevenue = await _doctorService.GetDoctorTotalRevenueAsync(id.Value);
        ViewBag.TotalAppointments = await _doctorService.GetDoctorAppointmentCountAsync(id.Value);

        return View(doctor);
    }

    // GET: Doctors/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Doctors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,Specialization,ContactNumber,Email,LicenseNumber,Address,ConsultationFee,IsActive")] Doctor doctor)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _doctorService.CreateDoctorAsync(doctor);
                TempData["SuccessMessage"] = "Doctor added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating doctor: {ex.Message}");
            }
        }
        return View(doctor);
    }

    // GET: Doctors/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var doctor = await _doctorService.GetDoctorByIdAsync(id.Value);
        if (doctor == null)
        {
            return NotFound();
        }
        return View(doctor);
    }

    // POST: Doctors/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Specialization,ContactNumber,Email,LicenseNumber,Address,ConsultationFee,IsActive")] Doctor doctor)
    {
        if (id != doctor.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _doctorService.UpdateDoctorAsync(doctor);
                TempData["SuccessMessage"] = "Doctor updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (!await DoctorExists(doctor.Id))
                {
                    return NotFound();
                }
                ModelState.AddModelError("", $"Error updating doctor: {ex.Message}");
            }
        }
        return View(doctor);
    }

    // POST: Doctors/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
            {
                return Json(new { success = false, message = "Doctor not found." });
            }

            await _doctorService.DeleteDoctorAsync(id);
            TempData["SuccessMessage"] = "Doctor deleted successfully!";
            return Json(new { success = true, message = "Doctor deleted successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error deleting doctor: {ex.Message}" });
        }
    }

    private async Task<bool> DoctorExists(int id)
    {
        var doctor = await _doctorService.GetDoctorByIdAsync(id);
        return doctor != null;
    }
}

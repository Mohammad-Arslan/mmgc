using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMGC.Models;
using MMGC.Services;
using MMGC.Repositories;

namespace MMGC.Controllers;

[Authorize]
public class AppointmentsController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly IRepository<Nurse> _nurseRepository;

    public AppointmentsController(
        IAppointmentService appointmentService,
        IPatientService patientService,
        IDoctorService doctorService,
        IRepository<Nurse> nurseRepository)
    {
        _appointmentService = appointmentService;
        _patientService = patientService;
        _doctorService = doctorService;
        _nurseRepository = nurseRepository;
    }

    // GET: Appointments
    public async Task<IActionResult> Index()
    {
        var appointments = await _appointmentService.GetAllAppointmentsAsync();
        return View(appointments);
    }

    // GET: Appointments/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _appointmentService.GetAppointmentByIdAsync(id.Value);
        if (appointment == null)
        {
            return NotFound();
        }

        return View(appointment);
    }

    // GET: Appointments/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Doctors = await _doctorService.GetAllDoctorsAsync();
        ViewBag.Nurses = await _nurseRepository.GetAllAsync();
        return View();
    }

    // POST: Appointments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PatientId,DoctorId,NurseId,AppointmentDate,AppointmentType,Reason,Notes,ConsultationFee")] Appointment appointment)
    {
        if (ModelState.IsValid)
        {
            appointment.CreatedBy = User.Identity?.Name;
            appointment.Status = "Scheduled";
            await _appointmentService.CreateAppointmentAsync(appointment);
            return RedirectToAction(nameof(Index));
        }
        
        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Doctors = await _doctorService.GetAllDoctorsAsync();
        ViewBag.Nurses = await _nurseRepository.GetAllAsync();
        return View(appointment);
    }

    // GET: Appointments/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _appointmentService.GetAppointmentByIdAsync(id.Value);
        if (appointment == null)
        {
            return NotFound();
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Doctors = await _doctorService.GetAllDoctorsAsync();
        ViewBag.Nurses = await _nurseRepository.GetAllAsync();
        return View(appointment);
    }

    // POST: Appointments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,DoctorId,NurseId,AppointmentDate,AppointmentType,Status,Reason,Notes,ConsultationFee")] Appointment appointment)
    {
        if (id != appointment.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _appointmentService.UpdateAppointmentAsync(appointment);
            }
            catch
            {
                if (!await AppointmentExists(appointment.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Doctors = await _doctorService.GetAllDoctorsAsync();
        ViewBag.Nurses = await _nurseRepository.GetAllAsync();
        return View(appointment);
    }

    // GET: Appointments/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _appointmentService.GetAppointmentByIdAsync(id.Value);
        if (appointment == null)
        {
            return NotFound();
        }

        return View(appointment);
    }

    // POST: Appointments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _appointmentService.DeleteAppointmentAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // POST: Appointments/SendSMS/5
    [HttpPost]
    public async Task<IActionResult> SendSMS(int id)
    {
        var result = await _appointmentService.SendSMSNotificationAsync(id);
        return Json(new { success = result });
    }

    // POST: Appointments/SendWhatsApp/5
    [HttpPost]
    public async Task<IActionResult> SendWhatsApp(int id)
    {
        var result = await _appointmentService.SendWhatsAppNotificationAsync(id);
        return Json(new { success = result });
    }

    private async Task<bool> AppointmentExists(int id)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        return appointment != null;
    }
}

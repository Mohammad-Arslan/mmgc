using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IRepository<Appointment> _repository;
    private readonly ApplicationDbContext _context;

    public AppointmentService(IRepository<Appointment> repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<Appointment>> GetAllAppointmentsAsync()
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Nurse)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Nurse)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByDateAsync(DateTime date)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Nurse)
            .Where(a => a.AppointmentDate.Date == date.Date)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetTodayAppointmentsAsync()
    {
        return await GetAppointmentsByDateAsync(DateTime.Today);
    }

    public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
    {
        appointment.CreatedDate = DateTime.Now;
        
        // Use context directly to avoid navigation property issues
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();
        return appointment;
    }

    public async Task UpdateAppointmentAsync(Appointment appointment)
    {
        appointment.UpdatedDate = DateTime.Now;
        // Use context directly to avoid navigation property issues
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAppointmentAsync(int id)
    {
        var appointment = await _repository.GetByIdAsync(id);
        if (appointment != null)
        {
            await _repository.DeleteAsync(appointment);
        }
    }

    public async Task<bool> SendSMSNotificationAsync(int appointmentId)
    {
        var appointment = await GetAppointmentByIdAsync(appointmentId);
        if (appointment == null || appointment.Patient == null)
            return false;

        // TODO: Implement actual SMS sending logic (Twilio, etc.)
        // For now, just mark as sent
        appointment.SMSSent = true;
        await _repository.UpdateAsync(appointment);
        return true;
    }

    public async Task<bool> SendWhatsAppNotificationAsync(int appointmentId)
    {
        var appointment = await GetAppointmentByIdAsync(appointmentId);
        if (appointment == null || appointment.Patient == null)
            return false;

        // TODO: Implement actual WhatsApp sending logic
        // For now, just mark as sent
        appointment.WhatsAppSent = true;
        await _repository.UpdateAsync(appointment);
        return true;
    }
}

using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class DoctorService : IDoctorService
{
    private readonly IRepository<Doctor> _repository;
    private readonly ApplicationDbContext _context;

    public DoctorService(IRepository<Doctor> repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
    {
        return await _context.Doctors
            .OrderBy(d => d.FirstName)
            .ThenBy(d => d.LastName)
            .ToListAsync();
    }

    public async Task<Doctor?> GetDoctorByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Doctor?> GetDoctorWithDetailsAsync(int id)
    {
        return await _context.Doctors
            .Include(d => d.Appointments)
            .Include(d => d.Procedures)
            .Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Doctor> CreateDoctorAsync(Doctor doctor)
    {
        doctor.CreatedDate = DateTime.Now;
        return await _repository.AddAsync(doctor);
    }

    public async Task UpdateDoctorAsync(Doctor doctor)
    {
        doctor.UpdatedDate = DateTime.Now;
        await _repository.UpdateAsync(doctor);
    }

    public async Task DeleteDoctorAsync(int id)
    {
        var doctor = await _repository.GetByIdAsync(id);
        if (doctor != null)
        {
            await _repository.DeleteAsync(doctor);
        }
    }

    public async Task<decimal> GetDoctorTotalRevenueAsync(int doctorId)
    {
        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "Completed")
            .SumAsync(a => a.ConsultationFee);

        var procedures = await _context.Procedures
            .Where(p => p.DoctorId == doctorId && p.Status == "Completed")
            .SumAsync(p => p.ProcedureFee);

        return appointments + procedures;
    }

    public async Task<int> GetDoctorAppointmentCountAsync(int doctorId)
    {
        return await _context.Appointments
            .CountAsync(a => a.DoctorId == doctorId);
    }
}

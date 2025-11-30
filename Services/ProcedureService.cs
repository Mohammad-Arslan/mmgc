using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class ProcedureService : IProcedureService
{
    private readonly IRepository<Procedure> _repository;
    private readonly ApplicationDbContext _context;

    public ProcedureService(IRepository<Procedure> repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<Procedure>> GetAllProceduresAsync()
    {
        return await _context.Procedures
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Nurse)
            .OrderByDescending(p => p.ProcedureDate)
            .ToListAsync();
    }

    public async Task<Procedure?> GetProcedureByIdAsync(int id)
    {
        return await _context.Procedures
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Nurse)
            .Include(p => p.LabTests)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Procedure> CreateProcedureAsync(Procedure procedure)
    {
        procedure.CreatedDate = DateTime.Now;
        return await _repository.AddAsync(procedure);
    }

    public async Task UpdateProcedureAsync(Procedure procedure)
    {
        procedure.UpdatedDate = DateTime.Now;
        await _repository.UpdateAsync(procedure);
    }

    public async Task DeleteProcedureAsync(int id)
    {
        var procedure = await _repository.GetByIdAsync(id);
        if (procedure != null)
        {
            await _repository.DeleteAsync(procedure);
        }
    }
}

using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class TransactionService : ITransactionService
{
    private readonly IRepository<Transaction> _repository;
    private readonly ApplicationDbContext _context;

    public TransactionService(IRepository<Transaction> repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<Transaction>> GetAllTransactionsAsync()
    {
        return await _context.Transactions
            .Include(t => t.Patient)
            .Include(t => t.Appointment)
            .Include(t => t.Procedure)
            .Include(t => t.LabTest)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        return await _context.Transactions
            .Include(t => t.Patient)
            .Include(t => t.Appointment)
            .Include(t => t.Procedure)
            .Include(t => t.LabTest)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        transaction.CreatedDate = DateTime.Now;
        transaction.TransactionDate = DateTime.Now;
        return await _repository.AddAsync(transaction);
    }

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        await _repository.UpdateAsync(transaction);
    }

    public async Task DeleteTransactionAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction != null)
        {
            await _repository.DeleteAsync(transaction);
        }
    }

    public async Task<string> GenerateInvoiceAsync(int transactionId)
    {
        var transaction = await GetTransactionByIdAsync(transactionId);
        if (transaction == null)
            throw new Exception("Transaction not found");

        // TODO: Implement actual invoice generation (PDF generation)
        // For now, just mark as generated
        transaction.InvoiceGenerated = true;
        transaction.InvoicePath = $"invoices/invoice_{transactionId}_{DateTime.Now:yyyyMMdd}.pdf";
        await _repository.UpdateAsync(transaction);

        return transaction.InvoicePath;
    }
}

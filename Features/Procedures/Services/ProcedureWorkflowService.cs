using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MMGC.Data;
using MMGC.Models;
using MMGC.Shared.DTOs;
using MMGC.Shared.Enums;
using MMGC.Shared.Exceptions;
using MMGC.Shared.Interfaces;

namespace MMGC.Features.Procedures.Services;

public class ProcedureWorkflowService : IProcedureWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProcedureWorkflowService> _logger;

    public ProcedureWorkflowService(ApplicationDbContext context, INotificationService notificationService, ILogger<ProcedureWorkflowService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ProcedureRequestDto> CreateProcedureRequestAsync(int patientId, int? doctorId, string procedureType, string reason, DateTime? requestedDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
            if (patient == null)
                throw new EntityNotFoundException(nameof(Models.Patient), patientId);

            var request = new ProcedureRequest
            {
                PatientId = patientId,
                DoctorId = doctorId,
                ProcedureType = procedureType,
                ReasonForProcedure = reason,
                RequestedDate = requestedDate,
                Status = ProcedureStatusEnum.Requested,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "Patient"
            };

            _context.ProcedureRequests.Add(request);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Procedure request created: ID={RequestId}, Type={ProcedureType}, Patient={PatientId}", request.Id, procedureType, patientId);
            return MapToDto(request, patient);
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating procedure request for patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<ProcedureRequestDto?> GetProcedureRequestAsync(int procedureRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _context.ProcedureRequests
                .Include(pr => pr.Patient)
                .Include(pr => pr.AssignedDoctor)
                .FirstOrDefaultAsync(pr => pr.Id == procedureRequestId, cancellationToken);

            return request == null ? null : MapToDto(request, request.Patient!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving procedure request {RequestId}", procedureRequestId);
            throw;
        }
    }

    public async Task<(List<ProcedureRequestDto> Items, int TotalCount)> GetPendingRequestsForDoctorAsync(int doctorId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.ProcedureRequests
                .AsNoTracking()
                .Where(pr => pr.Status == ProcedureStatusEnum.Requested && pr.DoctorId == doctorId)
                .OrderByDescending(pr => pr.CreatedDate);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(pr => pr.Patient)
                .Include(pr => pr.AssignedDoctor)
                .Select(pr => new ProcedureRequestDto
                {
                    Id = pr.Id,
                    PatientId = pr.PatientId,
                    PatientName = pr.Patient!.FullName,
                    DoctorId = pr.DoctorId,
                    DoctorName = pr.AssignedDoctor != null ? pr.AssignedDoctor.FullName : "Unassigned",
                    ProcedureType = pr.ProcedureType,
                    ReasonForProcedure = pr.ReasonForProcedure,
                    RequestedDate = pr.RequestedDate,
                    Status = pr.Status.ToString(),
                    CreatedDate = pr.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending requests for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<(List<ProcedureRequestDto> Items, int TotalCount)> GetPatientProcedureRequestsAsync(int patientId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.ProcedureRequests
                .AsNoTracking()
                .Where(pr => pr.PatientId == patientId)
                .OrderByDescending(pr => pr.CreatedDate);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(pr => pr.Patient)
                .Include(pr => pr.AssignedDoctor)
                .Select(pr => new ProcedureRequestDto
                {
                    Id = pr.Id,
                    PatientId = pr.PatientId,
                    PatientName = pr.Patient!.FullName,
                    DoctorId = pr.DoctorId,
                    DoctorName = pr.AssignedDoctor != null ? pr.AssignedDoctor.FullName : null,
                    ProcedureType = pr.ProcedureType,
                    ReasonForProcedure = pr.ReasonForProcedure,
                    RequestedDate = pr.RequestedDate,
                    Status = pr.Status.ToString(),
                    ApprovalComments = pr.ApprovalComments,
                    CreatedDate = pr.CreatedDate,
                    ReviewedDate = pr.ReviewedDate
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving procedure requests for patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<ProcedureRequestDto> ApproveProcedureRequestAsync(int procedureRequestId, int doctorId, string? approvalComments = null, DateTime? scheduledDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _context.ProcedureRequests
                .Include(pr => pr.Patient)
                .Include(pr => pr.AssignedDoctor)
                .FirstOrDefaultAsync(pr => pr.Id == procedureRequestId, cancellationToken);

            if (request == null)
                throw new EntityNotFoundException(nameof(ProcedureRequest), procedureRequestId);

            if (request.Status != ProcedureStatusEnum.Requested)
                throw new InvalidProcedureStateTransitionException(request.Status.ToString(), "Approved");

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);
            if (doctor == null)
                throw new EntityNotFoundException(nameof(Models.Doctor), doctorId);

            request.Status = ProcedureStatusEnum.Approved;
            request.DoctorId = doctorId;
            request.ApprovalComments = approvalComments;
            request.ReviewedDate = DateTime.UtcNow;

            if (scheduledDate.HasValue)
            {
                request.Status = ProcedureStatusEnum.Scheduled;
                request.ScheduledDate = scheduledDate.Value;

                var procedure = new Procedure
                {
                    PatientId = request.PatientId,
                    DoctorId = doctorId,
                    ProcedureName = request.ProcedureType,
                    ProcedureType = request.ProcedureType,
                    ProcedureDate = scheduledDate.Value,
                    Status = "Scheduled",
                    CreatedDate = DateTime.Now,
                    CreatedBy = $"Doctor_{doctorId}"
                };

                _context.Procedures.Add(procedure);
                await _context.SaveChangesAsync(cancellationToken);
                request.LinkedProcedureId = procedure.Id;
            }

            _context.ProcedureRequests.Update(request);
            await _context.SaveChangesAsync(cancellationToken);
            await _notificationService.SendProcedureApprovedAsync(procedureRequestId, cancellationToken);

            _logger.LogInformation("Procedure request approved: ID={RequestId}, Doctor={DoctorId}, Scheduled={Scheduled}", procedureRequestId, doctorId, scheduledDate.HasValue);
            return MapToDto(request, request.Patient!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving procedure request {RequestId}", procedureRequestId);
            throw;
        }
    }

    public async Task<ProcedureRequestDto> RejectProcedureRequestAsync(int procedureRequestId, int doctorId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _context.ProcedureRequests
                .Include(pr => pr.Patient)
                .FirstOrDefaultAsync(pr => pr.Id == procedureRequestId, cancellationToken);

            if (request == null)
                throw new EntityNotFoundException(nameof(ProcedureRequest), procedureRequestId);

            if (request.Status != ProcedureStatusEnum.Requested)
                throw new InvalidProcedureStateTransitionException(request.Status.ToString(), "Rejected");

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);
            if (doctor == null)
                throw new EntityNotFoundException(nameof(Models.Doctor), doctorId);

            request.Status = ProcedureStatusEnum.Rejected;
            request.DoctorId = doctorId;
            request.ApprovalComments = rejectionReason;
            request.ReviewedDate = DateTime.UtcNow;

            _context.ProcedureRequests.Update(request);
            await _context.SaveChangesAsync(cancellationToken);
            await _notificationService.SendProcedureRejectedAsync(procedureRequestId, rejectionReason, cancellationToken);

            _logger.LogInformation("Procedure request rejected: ID={RequestId}, Doctor={DoctorId}", procedureRequestId, doctorId);
            return MapToDto(request, request.Patient!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting procedure request {RequestId}", procedureRequestId);
            throw;
        }
    }

    public async Task<int> ScheduleApprovedProcedureAsync(int procedureRequestId, DateTime scheduledDateTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _context.ProcedureRequests.FirstOrDefaultAsync(pr => pr.Id == procedureRequestId, cancellationToken);
            if (request == null)
                throw new EntityNotFoundException(nameof(ProcedureRequest), procedureRequestId);

            if (request.Status != ProcedureStatusEnum.Approved)
                throw new InvalidProcedureStateTransitionException(request.Status.ToString(), "Scheduled");

            if (request.DoctorId == null)
                throw new BusinessRuleViolationException("Procedure must be approved by a doctor before scheduling");

            var procedure = new Procedure
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId.Value,
                ProcedureName = request.ProcedureType,
                ProcedureType = request.ProcedureType,
                ProcedureDate = scheduledDateTime,
                Status = "Scheduled",
                CreatedDate = DateTime.Now,
                CreatedBy = $"ProcedureWorkflow_{procedureRequestId}"
            };

            _context.Procedures.Add(procedure);
            request.Status = ProcedureStatusEnum.Scheduled;
            request.ScheduledDate = scheduledDateTime;
            request.LinkedProcedureId = procedure.Id;

            _context.ProcedureRequests.Update(request);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Procedure scheduled: RequestID={RequestId}, ProcedureID={ProcedureId}, DateTime={DateTime}", procedureRequestId, procedure.Id, scheduledDateTime);
            return procedure.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling procedure request {RequestId}", procedureRequestId);
            throw;
        }
    }

    public async Task<ProcedureRequestDto> CancelProcedureRequestAsync(int procedureRequestId, string cancellationReason, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _context.ProcedureRequests
                .Include(pr => pr.Patient)
                .FirstOrDefaultAsync(pr => pr.Id == procedureRequestId, cancellationToken);

            if (request == null)
                throw new EntityNotFoundException(nameof(ProcedureRequest), procedureRequestId);

            if (request.Status == ProcedureStatusEnum.Completed)
                throw new InvalidProcedureStateTransitionException(request.Status.ToString(), "Cancelled");

            request.Status = ProcedureStatusEnum.Cancelled;
            request.ApprovalComments = cancellationReason;

            _context.ProcedureRequests.Update(request);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Procedure request cancelled: ID={RequestId}, Reason={Reason}", procedureRequestId, cancellationReason);
            return MapToDto(request, request.Patient!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling procedure request {RequestId}", procedureRequestId);
            throw;
        }
    }

    public async Task<ProcedureRequestDto> CompleteProcedureAsync(int procedureRequestId, string? completionNotes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _context.ProcedureRequests
                .Include(pr => pr.Patient)
                .Include(pr => pr.LinkedProcedure)
                .FirstOrDefaultAsync(pr => pr.Id == procedureRequestId, cancellationToken);

            if (request == null)
                throw new EntityNotFoundException(nameof(ProcedureRequest), procedureRequestId);

            if (request.Status != ProcedureStatusEnum.Scheduled)
                throw new InvalidProcedureStateTransitionException(request.Status.ToString(), "Completed");

            request.Status = ProcedureStatusEnum.Completed;
            if (!string.IsNullOrEmpty(completionNotes))
                request.ApprovalComments = completionNotes;

            if (request.LinkedProcedure != null)
            {
                request.LinkedProcedure.Status = "Completed";
                _context.Procedures.Update(request.LinkedProcedure);
            }

            _context.ProcedureRequests.Update(request);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Procedure completed: ID={RequestId}", procedureRequestId);
            return MapToDto(request, request.Patient!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing procedure request {RequestId}", procedureRequestId);
            throw;
        }
    }

    private static ProcedureRequestDto MapToDto(ProcedureRequest request, Patient patient)
    {
        return new ProcedureRequestDto
        {
            Id = request.Id,
            PatientId = request.PatientId,
            PatientName = patient.FullName,
            DoctorId = request.DoctorId,
            DoctorName = request.AssignedDoctor?.FullName,
            ProcedureType = request.ProcedureType,
            ReasonForProcedure = request.ReasonForProcedure,
            RequestedDate = request.RequestedDate,
            Status = request.Status.ToString(),
            ApprovalComments = request.ApprovalComments,
            CreatedDate = request.CreatedDate,
            ReviewedDate = request.ReviewedDate,
            LinkedProcedureId = request.LinkedProcedureId
        };
    }
}

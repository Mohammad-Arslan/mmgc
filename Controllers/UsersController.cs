using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Models;
using MMGC.Services;

namespace MMGC.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IDoctorService _doctorService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IDoctorService doctorService,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _doctorService = doctorService;
        _logger = logger;
    }

    // GET: Users
    public async Task<IActionResult> Index(string searchString)
    {
        var users = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            users = users.Where(u =>
                u.FirstName!.Contains(searchString) ||
                u.LastName!.Contains(searchString) ||
                u.Email!.Contains(searchString) ||
                u.UserName!.Contains(searchString));
        }

        var userList = await users.ToListAsync();
        var usersWithRoles = new List<UserViewModel>();

        foreach (var user in userList)
        {
            var roles = await _userManager.GetRolesAsync(user);
            usersWithRoles.Add(new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName!,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                Roles = roles.ToList()
            });
        }

        if (!string.IsNullOrEmpty(searchString))
        {
            ViewBag.SearchString = searchString;
        }

        return View(usersWithRoles);
    }

    // GET: Users/Details/5
    public async Task<IActionResult> Details(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var viewModel = new UserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            UserName = user.UserName!,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            Roles = roles.ToList()
        };

        return View(viewModel);
    }

    // GET: Users/Create
    public async Task<IActionResult> Create()
    {
        await PopulateRolesAsync();
        return View();
    }

    // POST: Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                await PopulateRolesAsync();
                return View(model);
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "A user with this email already exists.");
                await PopulateRolesAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = model.EmailConfirmed
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created successfully", model.Email);
                
                // Add user to role
                if (!string.IsNullOrEmpty(model.Role))
                {
                    var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogWarning("Failed to add user {Email} to role {Role}. Errors: {Errors}", 
                            model.Email, model.Role, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        ModelState.AddModelError("", $"Failed to assign role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                    else
                    {
                        _logger.LogInformation("User {Email} added to role {Role}", model.Email, model.Role);
                    }
                    
                    // Always create profile record based on role, even if role assignment had issues
                    // This ensures the profile exists
                    try
                    {
                        await CreateOrUpdateProfileRecordAsync(user, model.Role, model);
                        _logger.LogInformation("Profile created for user {Email} with role {Role}", model.Email, model.Role);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating profile for user {Email} with role {Role}", model.Email, model.Role);
                        // Don't fail user creation if profile creation fails - user can still be created
                        ModelState.AddModelError("", $"User created but profile creation failed: {ex.Message}");
                    }
                }

                if (ModelState.ErrorCount == 0)
                {
                    TempData["SuccessMessage"] = "User and profile created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // User created but there were warnings
                    TempData["WarningMessage"] = "User created successfully, but some issues occurred. Please check the user details.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // User creation failed
            foreach (var error in result.Errors)
            {
                _logger.LogWarning("User creation error for {Email}: {Error}", model.Email, error.Description);
                ModelState.AddModelError("", error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating user {Email}", model.Email);
            ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
        }

        await PopulateRolesAsync();
        return View(model);
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault() ?? string.Empty;
        
        // Check if profile exists, if not, create it automatically
        if (!string.IsNullOrEmpty(currentRole) && !await ProfileExistsAsync(user.Id, currentRole))
        {
            _logger.LogInformation("Profile missing for user {Email} with role {Role}, creating now", user.Email, currentRole);
            await CreateOrUpdateProfileRecordAsync(user, currentRole, new UserEditViewModel
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber
            });
        }

        var viewModel = new UserEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Role = currentRole,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled
        };

        await PopulateRolesAsync();
        return View(viewModel);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if email is being changed and if it already exists
            if (user.Email != model.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != id)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    await PopulateRolesAsync();
                    return View(model);
                }
            }

            // Update user properties
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.EmailConfirmed = model.EmailConfirmed;
            user.LockoutEnabled = model.LockoutEnabled;

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                // Update password if provided
                if (!string.IsNullOrEmpty(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                        {
                            ModelState.AddModelError("", $"Password error: {error.Description}");
                        }
                        await PopulateRolesAsync();
                        return View(model);
                    }
                }

                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                var oldRole = currentRoles.FirstOrDefault();
                
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    
                    // Create or update profile record based on role
                    // If role changed, create new profile for new role
                    await CreateOrUpdateProfileRecordAsync(user, model.Role, model, oldRole);
                }

                TempData["SuccessMessage"] = "User and profile updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        await PopulateRolesAsync();
        return View(model);
    }

    // GET: Users/Delete/5
    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var viewModel = new UserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            UserName = user.UserName!,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            Roles = roles.ToList()
        };

        return View(viewModel);
    }

    // POST: Users/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Prevent deleting the current user
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "User deleted successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Error deleting user: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateRolesAsync()
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        ViewBag.Roles = new SelectList(roles, "Name", "Name");
    }

    /// <summary>
    /// Creates or updates a profile record in the appropriate table based on the user's role
    /// </summary>
    private async Task CreateOrUpdateProfileRecordAsync(ApplicationUser user, string role, UserCreateViewModel model, string? oldRole = null)
    {
        _logger.LogInformation("CreateOrUpdateProfileRecordAsync called for user {Email} with role {Role}, UserId: {UserId}", 
            user.Email, role, user.Id);
        
        // Check if profile already exists for this role
        var profileExists = await ProfileExistsAsync(user.Id, role);
        _logger.LogDebug("ProfileExistsAsync returned {Exists} for user {Email} with role {Role}", profileExists, user.Email, role);
        
        if (profileExists)
        {
            _logger.LogInformation("Profile exists, updating for user {Email} with role {Role}", user.Email, role);
            // Update existing profile
            await UpdateProfileRecordAsync(user, role, model);
            return;
        }

        // If role changed, we might need to handle old profile (optional - you can keep both or remove old one)
        if (!string.IsNullOrEmpty(oldRole) && oldRole != role)
        {
            _logger.LogInformation("User {Email} role changed from {OldRole} to {NewRole}", user.Email, oldRole, role);
        }

        // Create new profile
        _logger.LogInformation("Creating new profile for user {Email} with role {Role}", user.Email, role);
        await CreateProfileRecordAsync(user, role, model);
        _logger.LogInformation("CreateProfileRecordAsync completed for user {Email} with role {Role}", user.Email, role);
    }

    /// <summary>
    /// Creates or updates a profile record from UserEditViewModel
    /// </summary>
    private async Task CreateOrUpdateProfileRecordAsync(ApplicationUser user, string role, UserEditViewModel model, string? oldRole = null)
    {
        // Check if profile already exists for this role
        if (await ProfileExistsAsync(user.Id, role))
        {
            // Update existing profile
            await UpdateProfileRecordAsync(user, role, model);
            return;
        }

        // If role changed, we might need to handle old profile
        if (!string.IsNullOrEmpty(oldRole) && oldRole != role)
        {
            _logger.LogInformation("User {Email} role changed from {OldRole} to {NewRole}", user.Email, oldRole, role);
        }

        // Create new profile
        await CreateProfileRecordAsync(user, role, model);
    }

    /// <summary>
    /// Checks if a profile exists for the given user and role
    /// </summary>
    private async Task<bool> ProfileExistsAsync(string userId, string role)
    {
        return role switch
        {
            "Doctor" => await _context.Doctors.AnyAsync(d => d.UserId == userId),
            "Nurse" => await _context.Nurses.AnyAsync(n => n.UserId == userId),
            "ReceptionStaff" => await _context.ReceptionStaffs.AnyAsync(r => r.UserId == userId),
            "AccountsStaff" => await _context.AccountsStaffs.AnyAsync(a => a.UserId == userId),
            "LabStaff" => await _context.ReceptionStaffs.AnyAsync(r => r.UserId == userId && r.Department == "Laboratory"),
            _ => false
        };
    }

    /// <summary>
    /// Updates an existing profile record
    /// </summary>
    private async Task UpdateProfileRecordAsync(ApplicationUser user, string role, UserCreateViewModel model)
    {
        try
        {
            switch (role)
            {
                case "Doctor":
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (doctor != null)
                    {
                        doctor.FirstName = model.FirstName;
                        doctor.LastName = model.LastName;
                        doctor.Email = model.Email;
                        doctor.ContactNumber = model.PhoneNumber;
                        doctor.Specialization = model.Specialization ?? doctor.Specialization ?? "General";
                        doctor.LicenseNumber = model.LicenseNumber ?? doctor.LicenseNumber;
                        doctor.ConsultationFee = model.ConsultationFee ?? doctor.ConsultationFee;
                        doctor.Address = model.Address ?? doctor.Address;
                        doctor.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    break;

                case "Nurse":
                    var nurse = await _context.Nurses.FirstOrDefaultAsync(n => n.UserId == user.Id);
                    if (nurse != null)
                    {
                        nurse.FirstName = model.FirstName;
                        nurse.LastName = model.LastName;
                        nurse.Email = model.Email;
                        nurse.ContactNumber = model.PhoneNumber;
                        nurse.Department = model.Department ?? nurse.Department;
                        nurse.LicenseNumber = model.LicenseNumber ?? nurse.LicenseNumber;
                        nurse.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    break;

                case "ReceptionStaff":
                case "LabStaff":
                    var receptionStaff = await _context.ReceptionStaffs.FirstOrDefaultAsync(r => r.UserId == user.Id);
                    if (receptionStaff != null)
                    {
                        receptionStaff.FirstName = model.FirstName;
                        receptionStaff.LastName = model.LastName;
                        receptionStaff.Email = model.Email;
                        receptionStaff.ContactNumber = model.PhoneNumber;
                        receptionStaff.Department = model.Department ?? (role == "LabStaff" ? "Laboratory" : "Reception");
                        receptionStaff.EmployeeId = model.EmployeeId ?? receptionStaff.EmployeeId;
                        receptionStaff.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    break;

                case "AccountsStaff":
                    var accountsStaff = await _context.AccountsStaffs.FirstOrDefaultAsync(a => a.UserId == user.Id);
                    if (accountsStaff != null)
                    {
                        accountsStaff.FirstName = model.FirstName;
                        accountsStaff.LastName = model.LastName;
                        accountsStaff.Email = model.Email;
                        accountsStaff.ContactNumber = model.PhoneNumber;
                        accountsStaff.Department = model.Department ?? "Accounts";
                        accountsStaff.EmployeeId = model.EmployeeId ?? accountsStaff.EmployeeId;
                        accountsStaff.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile record for user {Email} with role {Role}", user.Email, role);
        }
    }

    /// <summary>
    /// Updates an existing profile record from UserEditViewModel
    /// </summary>
    private async Task UpdateProfileRecordAsync(ApplicationUser user, string role, UserEditViewModel model)
    {
        try
        {
            switch (role)
            {
                case "Doctor":
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (doctor != null)
                    {
                        doctor.FirstName = model.FirstName;
                        doctor.LastName = model.LastName;
                        doctor.Email = model.Email;
                        doctor.ContactNumber = model.PhoneNumber;
                        doctor.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    break;

                case "Nurse":
                    var nurse = await _context.Nurses.FirstOrDefaultAsync(n => n.UserId == user.Id);
                    if (nurse != null)
                    {
                        nurse.FirstName = model.FirstName;
                        nurse.LastName = model.LastName;
                        nurse.Email = model.Email;
                        nurse.ContactNumber = model.PhoneNumber;
                        nurse.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    break;

                case "ReceptionStaff":
                case "LabStaff":
                    var receptionStaff = await _context.ReceptionStaffs.FirstOrDefaultAsync(r => r.UserId == user.Id);
                    if (receptionStaff != null)
                    {
                        receptionStaff.FirstName = model.FirstName;
                        receptionStaff.LastName = model.LastName;
                        receptionStaff.Email = model.Email;
                        receptionStaff.ContactNumber = model.PhoneNumber;
                        receptionStaff.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    break;

                case "AccountsStaff":
                    var accountsStaff = await _context.AccountsStaffs.FirstOrDefaultAsync(a => a.UserId == user.Id);
                    if (accountsStaff != null)
                    {
                        accountsStaff.FirstName = model.FirstName;
                        accountsStaff.LastName = model.LastName;
                        accountsStaff.Email = model.Email;
                        accountsStaff.ContactNumber = model.PhoneNumber;
                        accountsStaff.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile record for user {Email} with role {Role}", user.Email, role);
        }
    }

    /// <summary>
    /// Creates a profile record in the appropriate table based on the user's role
    /// </summary>
    private async Task CreateProfileRecordAsync(ApplicationUser user, string role, UserCreateViewModel model)
    {
        try
        {
            switch (role)
            {
                case "Doctor":
                    var doctor = new Doctor
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = model.PhoneNumber,
                        Specialization = model.Specialization ?? "General",
                        LicenseNumber = model.LicenseNumber,
                        ConsultationFee = model.ConsultationFee ?? 0,
                        Address = model.Address,
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    await _doctorService.CreateDoctorAsync(doctor);
                    _logger.LogInformation("Created Doctor profile for user {Email}", model.Email);
                    break;

                case "Nurse":
                    _logger.LogInformation("Starting Nurse profile creation for user {Email} with UserId {UserId}", model.Email, user.Id);
                    // Truncate phone number to 15 characters to match database constraint
                    var contactNumber = model.PhoneNumber;
                    if (!string.IsNullOrEmpty(contactNumber) && contactNumber.Length > 15)
                    {
                        contactNumber = contactNumber.Substring(0, 15);
                        _logger.LogWarning("Phone number truncated to 15 characters for user {Email}", model.Email);
                    }
                    
                    var nurse = new Nurse
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = contactNumber,
                        Department = model.Department ?? "Nursing",
                        LicenseNumber = model.LicenseNumber,
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _logger.LogDebug("Nurse object created: FirstName={FirstName}, LastName={LastName}, Email={Email}, UserId={UserId}", 
                        nurse.FirstName, nurse.LastName, nurse.Email, nurse.UserId);
                    
                    _context.Nurses.Add(nurse);
                    _logger.LogDebug("Nurse added to context, calling SaveChangesAsync...");
                    
                    try
                    {
                        var saveResult = await _context.SaveChangesAsync();
                        _logger.LogInformation("SaveChangesAsync completed. Rows affected: {RowsAffected}. Nurse ID: {NurseId} for user {Email} with UserId {UserId}", 
                            saveResult, nurse.Id, model.Email, user.Id);
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, "SaveChangesAsync failed for Nurse profile. User: {Email}, UserId: {UserId}", model.Email, user.Id);
                        throw; // Re-throw to be caught by outer try-catch
                    }
                    
                    // Verify the profile was created correctly
                    var verifyNurse = await _context.Nurses.FirstOrDefaultAsync(n => n.UserId == user.Id);
                    if (verifyNurse == null)
                    {
                        _logger.LogError("Nurse profile verification failed! Profile was not saved for user {Email} with UserId {UserId}", model.Email, user.Id);
                        throw new Exception($"Failed to save nurse profile to database. UserId: {user.Id}, Email: {model.Email}");
                    }
                    else
                    {
                        _logger.LogInformation("Nurse profile verified successfully. Nurse ID: {NurseId}, UserId: {UserId}", verifyNurse.Id, user.Id);
                    }
                    break;

                case "ReceptionStaff":
                    var receptionStaff = new ReceptionStaff
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = model.PhoneNumber,
                        Department = model.Department ?? "Reception",
                        EmployeeId = model.EmployeeId,
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _context.ReceptionStaffs.Add(receptionStaff);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created ReceptionStaff profile for user {Email}", model.Email);
                    break;

                case "AccountsStaff":
                    var accountsStaff = new AccountsStaff
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = model.PhoneNumber,
                        Department = model.Department ?? "Accounts",
                        EmployeeId = model.EmployeeId,
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _context.AccountsStaffs.Add(accountsStaff);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created AccountsStaff profile for user {Email}", model.Email);
                    break;

                case "LabStaff":
                    // LabStaff can use a generic staff model or create a specific one
                    // For now, we'll create a ReceptionStaff-like record or you can create a LabStaff model
                    var labStaff = new ReceptionStaff
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = model.PhoneNumber,
                        Department = model.Department ?? "Laboratory",
                        EmployeeId = model.EmployeeId,
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _context.ReceptionStaffs.Add(labStaff);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created LabStaff profile for user {Email}", model.Email);
                    break;

                // Admin and Patient roles don't need separate profile tables
                case "Admin":
                case "Patient":
                    _logger.LogInformation("No profile record needed for role {Role}", role);
                    break;

                default:
                    _logger.LogWarning("Unknown role {Role} for user {Email}", role, model.Email);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile record for user {Email} with role {Role}", model.Email, role);
            // Don't throw - user is already created, profile can be created later
        }
    }

    /// <summary>
    /// Creates a profile record from UserEditViewModel
    /// </summary>
    private async Task CreateProfileRecordAsync(ApplicationUser user, string role, UserEditViewModel model)
    {
        try
        {
            switch (role)
            {
                case "Doctor":
                    var doctor = new Doctor
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = model.PhoneNumber,
                        Specialization = "General",
                        ConsultationFee = 0,
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    await _doctorService.CreateDoctorAsync(doctor);
                    _logger.LogInformation("Created Doctor profile for user {Email}", model.Email);
                    break;

                case "Nurse":
                    var nurse = new Nurse
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = model.PhoneNumber,
                        Department = "Nursing",
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _context.Nurses.Add(nurse);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created Nurse profile for user {Email}", model.Email);
                    break;

                case "ReceptionStaff":
                    var receptionStaff = new ReceptionStaff
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = model.PhoneNumber,
                        Department = "Reception",
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _context.ReceptionStaffs.Add(receptionStaff);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created ReceptionStaff profile for user {Email}", model.Email);
                    break;

                case "AccountsStaff":
                    var accountsStaff = new AccountsStaff
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = model.PhoneNumber,
                        Department = "Accounts",
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _context.AccountsStaffs.Add(accountsStaff);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created AccountsStaff profile for user {Email}", model.Email);
                    break;

                case "LabStaff":
                    var labStaff = new ReceptionStaff
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        ContactNumber = model.PhoneNumber,
                        Department = "Laboratory",
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _context.ReceptionStaffs.Add(labStaff);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created LabStaff profile for user {Email}", model.Email);
                    break;

                // Admin and Patient roles don't need separate profile tables
                case "Admin":
                case "Patient":
                    _logger.LogInformation("No profile record needed for role {Role}", role);
                    break;

                default:
                    _logger.LogWarning("Unknown role {Role} for user {Email}", role, model.Email);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile record for user {Email} with role {Role}", model.Email, role);
            // Don't throw - user is already created, profile can be created later
        }
    }
}

// Helper ViewModel for displaying users
public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public List<string> Roles { get; set; } = new();
}


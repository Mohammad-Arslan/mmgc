using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;

namespace MMGC.Data;

public static class DbInitializer
{
    /// <summary>Default password for seeded doctor and patient users. Meets Identity requirements (uppercase, lowercase, digit).</summary>
    private const string DefaultPassword = "Password1";

    public static async Task SeedRolesAndAdminUser(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create roles
        string[] roles = { "Admin", "Doctor", "Nurse", "LabStaff", "ReceptionStaff", "AccountsStaff", "Patient" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Create default admin user
        const string adminEmail = "admin@mmgc.com";
        const string adminPassword = "Admin@123";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User"
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else
        {
            // Ensure admin is in Admin role
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        await SeedLabTestCategoriesAsync(serviceProvider);
        await SeedDoctorsAndPatientsAsync(serviceProvider);
        await SeedProceduresAsync(serviceProvider);
    }

    public static async Task SeedLabTestCategoriesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await context.LabTestCategories.AnyAsync()) return;

        var categories = new[]
        {
            new LabTestCategory { CategoryName = "Blood Tests", Description = "Complete blood count, lipid panel, glucose", IsActive = true },
            new LabTestCategory { CategoryName = "Radiology", Description = "X-Ray, CT, MRI imaging", IsActive = true },
            new LabTestCategory { CategoryName = "Pathology", Description = "Biopsy, histology, cytology", IsActive = true },
            new LabTestCategory { CategoryName = "Ultrasound", Description = "Abdominal, cardiac, vascular ultrasound", IsActive = true },
            new LabTestCategory { CategoryName = "Urine Analysis", Description = "Urinalysis, culture", IsActive = true },
            new LabTestCategory { CategoryName = "Cardiac", Description = "ECG, stress test, echocardiogram", IsActive = true },
            new LabTestCategory { CategoryName = "Microbiology", Description = "Culture and sensitivity", IsActive = true },
            new LabTestCategory { CategoryName = "Biochemistry", Description = "Liver function, kidney function, electrolytes", IsActive = true }
        };
        context.LabTestCategories.AddRange(categories);
        await context.SaveChangesAsync();
    }

    public static async Task SeedDoctorsAndPatientsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (await context.Doctors.AnyAsync() && await context.Patients.AnyAsync()) return;

        // Ensure Patient role exists
        if (!await roleManager.RoleExistsAsync("Patient"))
            await roleManager.CreateAsync(new IdentityRole("Patient"));
        if (!await roleManager.RoleExistsAsync("Doctor"))
            await roleManager.CreateAsync(new IdentityRole("Doctor"));

        // Seed Doctors (with users)
        var doctorUsers = new List<(string Email, string FirstName, string LastName, string Specialization)>
        {
            ("dr.smith@mmgc.com", "John", "Smith", "General Surgery"),
            ("dr.johnson@mmgc.com", "Sarah", "Johnson", "Cardiology"),
            ("dr.williams@mmgc.com", "Michael", "Williams", "Orthopedics"),
            ("dr.brown@mmgc.com", "Emily", "Brown", "Pediatrics"),
            ("dr.davis@mmgc.com", "David", "Davis", "Ophthalmology")
        };

        foreach (var (email, firstName, lastName, special) in doctorUsers)
        {
            if (await userManager.FindByEmailAsync(email) != null) continue;
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };
            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, "Doctor");
        }

        // Seed Patients (with users)
        var patientUsers = new List<(string Email, string FirstName, string LastName, string MrNumber)>
        {
            ("patient1@mmgc.com", "Alice", "Anderson", "MR001"),
            ("patient2@mmgc.com", "Bob", "Miller", "MR002"),
            ("patient3@mmgc.com", "Carol", "Wilson", "MR003"),
            ("patient4@mmgc.com", "Daniel", "Taylor", "MR004"),
            ("patient5@mmgc.com", "Eva", "Martinez", "MR005")
        };

        foreach (var (email, firstName, lastName, mrNumber) in patientUsers)
        {
            if (await userManager.FindByEmailAsync(email) != null) continue;
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };
            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, "Patient");
        }

        // Create Doctor records
        if (!await context.Doctors.AnyAsync())
        {
            var doctors = new List<Doctor>();
            foreach (var (email, firstName, lastName, special) in doctorUsers)
            {
                var u = await userManager.FindByEmailAsync(email);
                if (u == null) continue;
                doctors.Add(new Doctor
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Specialization = special,
                    Email = email,
                    ContactNumber = "555-0100",
                    ConsultationFee = 150,
                    IsActive = true,
                    UserId = u.Id
                });
            }
            context.Doctors.AddRange(doctors);
            await context.SaveChangesAsync();
        }

        // Create Patient records
        if (!await context.Patients.AnyAsync())
        {
            var patients = new List<Patient>();
            var baseDate = new DateTime(1980, 1, 1);
            var mrNumbers = new[] { "MR001", "MR002", "MR003", "MR004", "MR005" };
            for (var i = 0; i < patientUsers.Count; i++)
            {
                var (email, firstName, lastName, mrNumber) = patientUsers[i];
                var u = await userManager.FindByEmailAsync(email);
                if (u == null) continue;
                patients.Add(new Patient
                {
                    MRNumber = mrNumber,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    ContactNumber = "555-0200",
                    DateOfBirth = baseDate.AddYears(i * 5),
                    Gender = i % 2 == 0 ? "Male" : "Female",
                    Address = $"{100 + i} Main St",
                    City = "City",
                    UserId = u.Id
                });
            }
            context.Patients.AddRange(patients);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedProceduresAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await context.Procedures.AnyAsync()) return;

        var doctors = await context.Doctors.Take(3).ToListAsync();
        var patients = await context.Patients.Take(3).ToListAsync();
        if (doctors.Count == 0 || patients.Count == 0) return;

        var procedureNames = new[]
        {
            "Appendectomy", "Cholecystectomy", "Hernia Repair", "Knee Replacement", "Hip Replacement",
            "Cataract Surgery", "Prostate Surgery", "Cesarean Section", "Hysterectomy", "Colonoscopy",
            "Endoscopy", "Laparoscopy", "Tonsillectomy", "Mastectomy", "Angioplasty"
        };

        var procedures = new List<Procedure>();
        var rnd = new Random(42);
        foreach (var name in procedureNames)
        {
            var doc = doctors[rnd.Next(doctors.Count)];
            var pat = patients[rnd.Next(patients.Count)];
            procedures.Add(new Procedure
            {
                PatientId = pat.Id,
                DoctorId = doc.Id,
                ProcedureName = name,
                ProcedureType = "Surgery",
                ProcedureDate = DateTime.Now.AddDays(rnd.Next(1, 30)),
                ProcedureFee = 500 + rnd.Next(0, 1500),
                Status = "Scheduled",
                CreatedBy = "Seed"
            });
        }
        context.Procedures.AddRange(procedures);
        await context.SaveChangesAsync();
    }
}

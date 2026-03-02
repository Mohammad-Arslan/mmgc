using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MMGC.Data;
using MMGC.Repositories;
using MMGC.Services;
using MMGC.Models;
using MMGC.Shared.Interfaces;
using MMGC.Features.Patients.Services;
using MMGC.Features.Search.Services;
using MMGC.Features.Appointments.Services;
using MMGC.Features.Procedures.Services;
using MMGC.Shared.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Required for Identity UI

// Configure form options for file uploads (10MB limit)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10MB
    options.ValueLengthLimit = int.MaxValue;
    options.ValueCountLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configure Entity Framework Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Identity with Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Use custom Account/Login instead of Identity UI (/Identity/Account/Login)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ===== AUTHORIZATION POLICIES =====
builder.Services.AddAuthorization(options =>
{
    // Patient role policy
    options.AddPolicy("PatientOnly", policy =>
        policy.RequireRole("Patient"));
    
    // Doctor role policy
    options.AddPolicy("DoctorOnly", policy =>
        policy.RequireRole("Doctor"));
    
    // Admin role policy
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    
    // Medical staff (Doctor, Nurse, etc.)
    options.AddPolicy("MedicalStaff", policy =>
        policy.RequireRole("Doctor", "Nurse"));
    
    // Clinical staff (Doctor, Nurse, ReceptionStaff)
    options.AddPolicy("ClinicalStaff", policy =>
        policy.RequireRole("Doctor", "Nurse", "ReceptionStaff"));
    
    // Finance staff
    options.AddPolicy("FinanceStaff", policy =>
        policy.RequireRole("AccountsStaff", "Admin"));
    
    // Patient or Medical Staff
    options.AddPolicy("PatientOrMedicalStaff", policy =>
        policy.RequireRole("Patient", "Doctor", "Nurse"));
});

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// ===== PHASE 1: EXISTING SERVICES =====
builder.Services.AddScoped<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IProcedureService, ProcedureService>();
builder.Services.AddScoped<ILabTestService, LabTestService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDoctorDashboardService, DoctorDashboardService>();

// ===== PHASE 2: FEATURE SERVICES =====
builder.Services.AddScoped<IPatientDashboardService, PatientDashboardService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<INotificationLogService, NotificationLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationProvider, SmsNotificationProvider>();
builder.Services.AddScoped<INotificationProvider, EmailNotificationProvider>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IProcedureWorkflowService, ProcedureWorkflowService>();

// ===== PHASE 3: PUBLIC WEBSITE & DOCUMENT SERVICES =====
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IDoctorDirectoryService, DoctorDirectoryService>();
builder.Services.AddScoped<IPublicWebsiteService, PublicWebsiteService>();
builder.Services.AddScoped<IDocumentDownloadService, DocumentDownloadService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "admin-login",
    pattern: "admin/login",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "home",
    pattern: "Home",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Public}/{action=Index}/{id?}");

app.MapRazorPages(); // Required for Identity UI

// Ensure database is migrated and seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Apply migrations (creates database if not exists; Visual Studio Package Manager: Update-Database)
        logger.LogInformation("Applying migrations...");
        await context.Database.MigrateAsync();
        
        // Wait a moment for migrations to complete
        await Task.Delay(1000);
        
        // Seed roles and admin user
        logger.LogInformation("Starting database seeding...");
        await DbInitializer.SeedRolesAndAdminUser(scope.ServiceProvider);
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
        // Don't exit - allow app to start even if seeding fails
    }
}

app.Run();

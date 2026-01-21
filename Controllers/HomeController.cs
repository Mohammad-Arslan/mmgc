using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MMGC.Models;
using MMGC.Data;

namespace MMGC.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                // For Doctor role, check if doctor profile exists before redirecting
                if (roles.Contains("Doctor"))
                {
                    // Allow access to DoctorDashboard even if profile not linked yet
                    // The dashboard will show an error message instead of redirecting
                    return RedirectToAction("Index", "DoctorDashboard");
                }
                
                return RedirectToRoleBasedDashboard(roles);
            }
        }
        return View();
    }

    /// <summary>
    /// Redirects user to appropriate dashboard based on their role
    /// </summary>
    private IActionResult RedirectToRoleBasedDashboard(IList<string> roles)
    {
        if (roles == null || roles.Count == 0)
        {
            return View();
        }

        // Check roles in priority order
        if (roles.Contains("Admin"))
        {
            return RedirectToAction("Dashboard", "Admin");
        }
        else if (roles.Contains("Doctor"))
        {
            return RedirectToAction("Index", "DoctorDashboard");
        }
        else if (roles.Contains("Nurse"))
        {
            return RedirectToAction("Dashboard", "Nurses");
        }
        else if (roles.Contains("ReceptionStaff"))
        {
            return RedirectToAction("Index", "Appointments");
        }
        else if (roles.Contains("AccountsStaff"))
        {
            return RedirectToAction("Index", "Transactions");
        }
        else if (roles.Contains("LabStaff"))
        {
            return RedirectToAction("Index", "LabTests");
        }
        else if (roles.Contains("Patient"))
        {
            return View(); // Stay on home page for patients
        }

        // Default fallback
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

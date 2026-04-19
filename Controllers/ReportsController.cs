using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRentalManagementSystem.Models;

namespace VehicleRentalManagementSystem.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly VehicleRentalDBContext _context;

        public ReportsController(VehicleRentalDBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Generate(string reportType, DateTime? startDate, DateTime? endDate, int? categoryId)
        {
            var model = new ReportViewModel
            {
                ReportType = reportType,
                StartDate = startDate,
                EndDate = endDate,
                CategoryId = categoryId,
                Categories = await _context.VehicleCategories.ToListAsync()
            };

            switch (reportType)
            {
                case "ReservationsByDate":
                    if (startDate == null || endDate == null)
                    {
                        ModelState.AddModelError("", "Please select a date range.");
                        return View("Index", model);
                    }
                    model.Reservations = await _context.Reservations
                        .Include(r => r.Customer)
                        .Include(r => r.Vehicle)
                        .Include(r => r.Billing)
                        .Where(r => r.StartDate >= startDate && r.EndDate <= endDate)
                        .OrderBy(r => r.StartDate)
                        .ToListAsync();
                    break;

                case "VehiclesByCategory":
                    model.Vehicles = await _context.Vehicles
                        .Include(v => v.Category)
                        .Where(v => categoryId == null || v.CategoryId == categoryId)
                        .OrderBy(v => v.Category.CategoryName)
                        .ToListAsync();
                    break;

                case "CustomerActivity":
                    model.Customers = await _context.Customers
                        .Include(c => c.Reservations)
                        .ThenInclude(r => r.Billing)
                        .OrderByDescending(c => c.Reservations.Count)
                        .ToListAsync();
                    break;
            }

            return View("Index", model);
        }
    }
}
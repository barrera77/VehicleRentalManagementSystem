using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VehicleRentalManagementSystem.Models;

namespace VehicleRentalManagementSystem.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly VehicleRentalDBContext _context;

        public ReservationsController(VehicleRentalDBContext context)
        {
            _context = context;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            var vehicleRentalDBContext = _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.Category)
                .Include(r => r.Billing); // optional: show billing in index if desired
            return View(await vehicleRentalDBContext.ToListAsync());
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.Category)
                .Include(r => r.Billing) // <-- ADD THIS
                .FirstOrDefaultAsync(m => m.ReservationId == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // GET: Reservations/Create
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email");
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate");
            return View();
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReservationId,CustomerId,VehicleId,StartDate,EndDate,Status,CreatedAt")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reservation);
                await _context.SaveChangesAsync();

                if (reservation.Status == "Confirmed")
                {
                    var billing = GenerateBilling(reservation);
                    _context.Billings.Add(billing);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", reservation.CustomerId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", reservation.VehicleId);
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", reservation.CustomerId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", reservation.VehicleId);
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReservationId,CustomerId,VehicleId,StartDate,EndDate,Status,CreatedAt")] Reservation reservation)
        {
            if (id != reservation.ReservationId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();

                    var billing = await _context.Billings.FirstOrDefaultAsync(b => b.ReservationId == reservation.ReservationId);

                    if (reservation.Status == "Confirmed")
                    {
                        if (billing == null)
                        {
                            var newBilling = GenerateBilling(reservation);
                            _context.Billings.Add(newBilling);
                        }
                        else
                        {
                            var updatedBilling = GenerateBilling(reservation);
                            updatedBilling.BillingId = billing.BillingId;
                            _context.Entry(billing).CurrentValues.SetValues(updatedBilling);
                        }
                        await _context.SaveChangesAsync();
                    }
                    else if (reservation.Status == "Cancelled" && billing != null)
                    {
                        billing.PaymentStatus = "Cancelled";
                        _context.Update(billing);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.ReservationId)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", reservation.CustomerId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", reservation.VehicleId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.Category)
                .Include(r => r.Billing) // <-- ADD THIS
                .FirstOrDefaultAsync(m => m.ReservationId == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);

                var billing = await _context.Billings.FirstOrDefaultAsync(b => b.ReservationId == reservation.ReservationId);
                if (billing != null)
                {
                    billing.PaymentStatus = "Cancelled";
                    _context.Update(billing);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.ReservationId == id);
        }

        private Billing GenerateBilling(Reservation reservation)
        {
            decimal baseRate = reservation.Vehicle.Category.DailyRate;
            int days = (reservation.EndDate - reservation.StartDate).Days + 1;

            decimal baseAmount = baseRate * days;
            decimal taxAmount = baseAmount * 0.13m;
            decimal additionalCharges = 0;
            decimal totalAmount = baseAmount + taxAmount + additionalCharges;

            return new Billing
            {
                ReservationId = reservation.ReservationId,
                BaseAmount = baseAmount,
                TaxAmount = taxAmount,
                AdditionalCharges = additionalCharges,
                TotalAmount = totalAmount,
                PaymentStatus = "Pending",
                BillingDate = DateTime.Now
            };
        }
    }
}
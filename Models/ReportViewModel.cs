namespace VehicleRentalManagementSystem.Models
{
    public class ReportViewModel
    {
        public string ReportType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CategoryId { get; set; }
        public List<VehicleCategory> Categories { get; set; } = new();
        public List<Reservation> Reservations { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
    }
}
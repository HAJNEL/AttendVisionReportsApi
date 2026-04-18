using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class HealthCheckService : IHealthCheckService
    {
        private readonly Data.AppDbContext _db;

        public HealthCheckService(Data.AppDbContext db)
        {
            _db = db;
        }

        public string CheckHealth()
        {
            try
            {
                using var connection = _db.Database.GetDbConnection();
                connection.Open();
                return "Healthy";
            }
            catch (System.Exception ex)
            {
                return $"Unhealthy (Exception: {ex.Message})";
            }
        }
    }
}

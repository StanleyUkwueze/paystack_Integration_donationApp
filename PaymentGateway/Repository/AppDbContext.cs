using Microsoft.EntityFrameworkCore;
using PaymentGateway.Models;

namespace PaymentGateway.Repository
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<TransactionModel> Transactions { get; set; }
    }
}

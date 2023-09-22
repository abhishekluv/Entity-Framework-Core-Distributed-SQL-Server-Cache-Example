using Microsoft.EntityFrameworkCore;

namespace EFCoreDistributedSQLServerCache.Models
{
    public class MyApplicationContext : DbContext
    {
        public MyApplicationContext(DbContextOptions<MyApplicationContext> options) : base(options) 
        {
            
        }

        public DbSet<Employee> Employees { get; set;}
    }
}

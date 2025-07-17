using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Demo;

public class Customer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? throw new InvalidOperationException("DB_CONNECTION not found");

        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customer");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}

public class CustomerService
{
    private readonly AppDbContext _dbContext;

    public CustomerService(AppDbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    // Part 3.1: Rewritten Code
    public async Task<Customer> GetCustomerInfo(string id, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Customer ID cannot be null or empty.", nameof(id));

        IQueryable<Customer> query = _dbContext.Customers.Where(c => c.Id == id);

        if (startDate.HasValue && endDate.HasValue)
        {
            query = query.Where(c => startDate <= c.CreatedAt && c.CreatedAt <= endDate);
        }

        var customer = await query
            .AsNoTracking()
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Customer with ID '{id}' not found or does not match the specified date range.");

        return customer;
    }

    // Part 3.2: Legacy Code
    // public DataTable GetCustomerInfo(string id, DateTime? startDate = null, DateTime? endDate = null)
    // {
    //     var customerTable = new DataTable();

    //     if (string.IsNullOrEmpty(id))
    //         return customerTable;

    //     using (var connection = new SqlConnection())
    //     {
    //         connection.Open();
    //         string sqlQuery = @"
    //             SELECT *
    //             FROM Customer
    //             WHERE id = @id
    //                 AND (
    //                     (@startDate IS NULL AND @endDate IS NULL)
    //                     OR
    //                     (@startDate IS NOT NULL
    //                     AND @endDate IS NOT NULL
    //                     AND created_at BETWEEN @startDate AND @endDate)
    //                 );";

    //         using (var command = new SqlCommand(sqlQuery, connection))
    //         {
    //             command.Parameters.Add("@id", SqlDbType.VarChar).Value = id;
    //             command.Parameters.Add("@startDate", SqlDbType.DateTime).Value = (object?)startDate ?? DBNull.Value;
    //             command.Parameters.Add("@endDate", SqlDbType.DateTime).Value = (object?)endDate ?? DBNull.Value;

    //             using (SqlDataAdapter adapter = new SqlDataAdapter(command))
    //             {
    //                 adapter.Fill(customerTable);
    //             }
    //         }
    //     }
    //     return customerTable;
    // }
}

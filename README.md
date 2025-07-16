# InterGold Junior Developer Technical Assessment
**Submitted by: Nithikorn Dejsongchan**

---

## Part 1: Analyze Legacy Code

### 1. What is the primary purpose of this function?

To retrieve customer information using a customer ID.

### 2. Identified Problems

- **Hardcoded Connection String:**
Embedding the connection string in code makes configuration and environment management difficult and insecure.

- **SQL Injection Vulnerability:**
The query concatenates user input directly, which is highly insecure and exposes the application to SQL injection.

- **Poor Maintainability:**
DataTable relies on string-based column access. This is error-prone because misspellings or database schema changes lead to runtime errors that are difficult to catch during development. In contrast, strongly-typed POCOs allow for compile-time checking, catching such errors immediately.

- **No Error Handling:**
The function does not catch or handle any exceptions, making failures hard to manage or log properly.

- **No Input Validation:**
The function proceeds to open a database connection even when the input ID is invalid or empty, leading to unnecessary resource usage.

### 3. Proposed Improvements

- **Use environment-based configuration:**
Move connection strings and sensitive settings to environment variables to improve security and allow for easier deployment across environments.

- **Introduce parameterized queries**
Prevent SQL injection by replacing raw query concatenation with safe, parameterized queries.

- **Refactor to use strongly-typed models**
Replace DataTable with object-oriented representations (e.g., POCO classes), improving type safety, readability, and maintainability.

- **Implement proper error handling**
Add structured exception handling and meaningful error messages to make debugging easier and improve user experience.

- **Add input validation**
Validate user input (e.g., required fields, correct format) before processing, to ensure data integrity and avoid unnecessary database operations.

---

## Part 2: Rewritten Function

**Chosen Language:** C# (.NET 6 with Entity Framework Core)

This function replaces raw SQL with a parameterized query using EF Core to prevent SQL injection.  
It also throw errors when something went wrong.

```csharp
public async Task<Customer> GetCustomerInfo(string id)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        throw new ArgumentException("Customer ID cannot be null or empty.", nameof(id));
    }

    var customer = await _dbContext.Customers
        .Where(c => c.Id == id)
        .FirstOrDefaultAsync()
        ?? throw new InvalidOperationException($"Customer with ID '{id}' not found or does not match the specified date range.");

    return customer;
}
```

---

## Part 3: Extended Logic - Date Filtering

The function accepts optional `startDate` and `endDate` parameters.  
If both are provided, it filters using `created_at BETWEEN @startDate AND @endDate`.  
Otherwise, it retrieves the customer by ID only.

This logic is implemented in both the modern function and adapted back into the legacy version (see below).

### 1. In Modern Code

```csharp
public async Task<Customer> GetCustomerInfo(string id, DateTime? startDate = null, DateTime? endDate = null)
{
    if (string.IsNullOrWhiteSpace(id))
        throw new ArgumentException("Customer ID cannot be null or empty.", nameof(id));

    IQueryable<Customer> query = _dbContext.Customers.Where(c => c.Id == id);

    if (startDate.HasValue && endDate.HasValue)
    {
        query = query.Where(c => startDate <= c.CreatedAt && c.CreatedAt <= endDate);
    }

    var customer = await query.FirstOrDefaultAsync()
        ?? throw new InvalidOperationException($"Customer with ID '{id}' not found or does not match the specified date range.");

    return customer;
}
```

### 2. In Legacy Code

```csharp
// var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

public DataTable GetCustomerInfo(string id, DateTime? startDate = null, DateTime? endDate = null)
{
    var customerTable = new DataTable();

    if (string.IsNullOrEmpty(id))
        return customerTable; // assumed that legacy caller not expected to handle exceptions
        
    using (var connection = new SqlConnection(connectionString))
    {
        connection.Open();
        string sqlQuery = @"
            SELECT *
            FROM Customer
            WHERE id = @id
                AND (
                    (@startDate IS NULL AND @endDate IS NULL)
                    OR
                    (@startDate IS NOT NULL
                    AND @endDate IS NOT NULL
                    AND created_at BETWEEN @startDate AND @endDate)
                );";

        using (var command = new SqlCommand(sqlQuery, connection))
        {
            command.Parameters.Add("@id", SqlDbType.VarChar).Value = id;
            command.Parameters.Add("@startDate", SqlDbType.DateTime).Value = (object?)startDate ?? DBNull.Value;
            command.Parameters.Add("@endDate", SqlDbType.DateTime).Value = (object?)endDate ?? DBNull.Value;

            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(customerTable);
            }
        }
    }
    return customerTable;
}
```

See the full version in [Program.cs](src/Program.cs)

---

## Part 4 (Optional): System Perspective

The function can be used by employees, such as customer service representatives, to search for customer information using a customer ID. The search interface may include input fields such as Customer ID, From Date, and To Date to filter customers who created their accounts within a specific time period.

Since the Customer ID is unique, the output is expected to return a single result. Therefore, displaying the information in a profile card format—rather than in a table or a list of transactions—would provide a clearer and more concise view of the customer’s details.

---

## Tools and Help

- Microsoft Docs – Referenced for .NET and EF Core usage.
- W3Schools – Referenced for SQL Syntax.
- QuillBot – Helped with paraphrasing and refining text.
- Gemini & ChatGPT – Assisted in understanding and simplifying official documentation.

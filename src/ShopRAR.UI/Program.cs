using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShopRAR.Domain;

namespace ShopRAR.UI;

public class Program
{
    
    private const string ConnectionString =
        "Server=tcp:shoprar-sql.database.windows.net,1433;" +
        "Initial Catalog=ShopRARdb;" +
        "User ID=shopraradmin;" +
        "Password=ShopRAR123;" +
        "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

    private static AdminUser? _currentAdmin = null;

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run --project ShopRAR.UI -- [linq|sproc]");
            Console.WriteLine("Defaulting to 'linq' (EF Core) for now.");
        }

        var mode = args.Length > 0 ? args[0].ToLower() : "linq";

        if (mode != "linq" && mode != "sproc")
        {
            Console.WriteLine($"Error: Unknown mode '{mode}'. Use 'linq' or 'sproc'.");
            return;
        }

        try
        {
            ServiceFactory.Initialize(mode, ConnectionString);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }

        var cosmosConnection = Environment.GetEnvironmentVariable("SHOPRAR_COSMOS_CONNECTION");
        if (!string.IsNullOrEmpty(cosmosConnection))
        {
            Console.WriteLine("Cosmos DB connection string found. Initializing logger...");
            CosmosLogger.Initialize(cosmosConnection);
        }
        else
        {
            Console.WriteLine("SHOPRAR_COSMOS_CONNECTION environment variable not set. Cosmos logging disabled.");
        }

        bool running = true;
        while (running)
        {
            Console.WriteLine($"\n=== ShopRAR E-Commerce System ({mode.ToUpper()}) ===");
            Console.WriteLine("=== Main Menu ===");
            Console.WriteLine("1. Customer Portal (Guest Checkout)");
            Console.WriteLine("2. Admin Portal (Login Required)");
            Console.WriteLine("3. Test Database Connection");
            Console.WriteLine("4. Exit");
            Console.Write("Select an option (1-4): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CustomerPortal();
                    break;
                case "2":
                    AdminPortal();
                    break;
                case "3":
                    TestDatabaseConnection();
                    break;
                case "4":
                    running = false;
                    Console.WriteLine("Goodbye!");
                    break;
                default:
                    Console.WriteLine("Invalid option. Please select 1-4.");
                    break;
            }
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
  
    private static int GetDefaultAdminId()
    {
        try
        {
            var adminService = ServiceFactory.CreateAdminUserService();
            var admin = adminService.GetAdminById(1);
            if (admin != null)
            {
                return 1;
            }
        }
        catch
        {
          
        }

        return 1;
    }
   
    private static void CustomerPortal()
    {
        bool back = false;
        while (!back)
        {
            Console.WriteLine("\n=== Customer Portal ===");
            Console.WriteLine("1. Browse Products");
            Console.WriteLine("2. View Shopping Cart");
            Console.WriteLine("3. Checkout");
            Console.WriteLine("4. Back to main menu");
            Console.Write("Select an option (1-4): ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    BrowseProducts().GetAwaiter().GetResult();
                    break;
                case "2":
                    ViewShoppingCart();
                    break;
                case "3":
                    Checkout().GetAwaiter().GetResult();
                    break;
                case "4":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static List<CartItem> _shoppingCart = new List<CartItem>();

    private class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
   
    private static void AdminPortal()
    {
        
        if (_currentAdmin != null)
        {
            AdminManagementMenu();
            return;
        }

        Console.WriteLine("\n=== Admin Login ===");
        Console.Write("Email: ");
        var email = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(email))
        {
            Console.WriteLine("Email cannot be empty.");
            return;
        }

        Console.Write("Password: ");
        var password = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Password cannot be empty.");
            return;
        }

        try
        {
            var adminService = ServiceFactory.CreateAdminUserService();
            var passwordHash = HashPassword(password);
            var admin = adminService.AuthenticateAdmin(email, passwordHash);

            if (admin != null)
            {
                _currentAdmin = admin;
                Console.WriteLine($"\nLogin successful! Welcome, {admin.Email}");
                AdminManagementMenu();
            }
            else
            {
                Console.WriteLine("\nInvalid email or password. Please try again.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nLogin error: {ex.Message}");
        }
    }

    private static void AdminManagementMenu()
    {
        bool logout = false;
        while (!logout)
        {
            Console.WriteLine($"\n=== Admin Management Portal (Logged in as: {_currentAdmin?.Email}) ===");
            Console.WriteLine("1. Customer Management");
            Console.WriteLine("2. Product Management");
            Console.WriteLine("3. Category Management");
            Console.WriteLine("4. Order Management");
            Console.WriteLine("5. Review Management");
            Console.WriteLine("6. Inventory Management");
            Console.WriteLine("7. Reports & Analytics");
            Console.WriteLine("8. Logout");
            Console.Write("Select an option (1-8): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CustomerMenu();
                    break;
                case "2":
                    ProductMenu();
                    break;
                case "3":
                    CategoryMenu();
                    break;
                case "4":
                    OrderMenu();
                    break;
                case "5":
                    ReviewMenu();
                    break;
                case "6":
                    InventoryMenu();
                    break;
                case "7":
                    ReportsMenu();
                    break;
                case "8":
                    _currentAdmin = null;
                    logout = true;
                    Console.WriteLine("Logged out successfully.");
                    break;
                default:
                    Console.WriteLine("Invalid option. Please select 1-8.");
                    break;
            }
        }
    }
   
    private static void CustomerMenu()
    {
        var service = ServiceFactory.CreateCustomerService();
        bool back = false;

        while (!back)
        {
            Console.WriteLine("\n=== Customer Management ===");
            Console.WriteLine("1. List all customers");
            Console.WriteLine("2. View customer by ID");
            Console.WriteLine("3. Search customers by name");
            Console.WriteLine("4. Create customer");
            Console.WriteLine("5. Update customer");
            Console.WriteLine("6. Delete customer");
            Console.WriteLine("7. Back to main menu");
            Console.Write("Select an option (1-7): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ListCustomers(service);
                    break;
                case "2":
                    ViewCustomer(service);
                    break;
                case "3":
                    SearchCustomers(service);
                    break;
                case "4":
                    CreateCustomer(service);
                    break;
                case "5":
                    UpdateCustomer(service);
                    break;
                case "6":
                    DeleteCustomer(service);
                    break;
                case "7":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void ListCustomers(ICustomerService service)
    {
        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = service.GetAllCustomersPaged(currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve customers.");
        
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine("\nNo customers found.");
                    return;
                }

                var customers = pagedResult.Items?.ToList() ?? new List<Customer>();
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + customers.Count, pagedResult.TotalCount);

                Console.WriteLine($"\nCustomers (Page {currentPage} of {pagedResult.TotalPages})");
                Console.WriteLine($"{"ID",-8} {"Name",-40} {"Email",-30} {"City",-20}");
                Console.WriteLine(new string('-', 100));
                
                foreach (var c in customers)
                {
                    var name = c.FullName?.Length > 38 ? c.FullName.Substring(0, 35) + "..." : c.FullName ?? "N/A";
                    var email = (c.Email ?? "").Length > 28 ? (c.Email ?? "").Substring(0, 25) + "..." : (c.Email ?? "N/A");
                    var city = (c.City ?? "").Length > 18 ? (c.City ?? "").Substring(0, 15) + "..." : (c.City ?? "N/A");
                    
                    Console.WriteLine($"{c.CustomerId,-8} {name,-40} {email,-30} {city,-20}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} customers");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  S - Select customer to view details");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "S":
                        Console.Write("Enter customer ID to view details: ");
                        var input = Console.ReadLine();
                        if (int.TryParse(input, out var customerId))
                        {
                            var customer = service.GetCustomerById(customerId);
                            if (customer != null)
                            {
                                Console.WriteLine($"\nID:          {customer.CustomerId}");
                                Console.WriteLine($"Name:        {customer.FullName}");
                                Console.WriteLine($"Email:       {customer.Email}");
                                Console.WriteLine($"Phone:       {customer.Phone}");
                                Console.WriteLine($"AddressLine: {customer.AddressLine}");
                                Console.WriteLine($"City:        {customer.City}");
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Customer ID not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid customer ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                        break;
                }
            }
        }
        catch (SqlException sqlEx)
        {
            Console.WriteLine($"\nERROR: SQL error while retrieving customers.");
            Console.WriteLine($"Error Number: {sqlEx.Number}");
            Console.WriteLine($"Error Message: {sqlEx.Message}");
            if (sqlEx.Number == -2) 
            {
                Console.WriteLine("\nThis appears to be a timeout error. Your database may be very large.");
                Console.WriteLine("Try using 'View customer by ID' instead to view specific customers.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: Failed to retrieve customers.");
            Console.WriteLine($"Error details: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private static void ViewCustomer(ICustomerService service)
    {
        Console.Write("Enter customer ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var c = service.GetCustomerById(id);
        if (c == null)
        {
            Console.WriteLine("Customer not found.");
            return;
        }

        Console.WriteLine($"\nID:          {c.CustomerId}");
        Console.WriteLine($"Name:        {c.FullName}");
        Console.WriteLine($"Email:       {c.Email}");
        Console.WriteLine($"Phone:       {c.Phone}");
        Console.WriteLine($"AddressLine: {c.AddressLine}");
        Console.WriteLine($"City:        {c.City}");
    }

    private static void SearchCustomers(ICustomerService service)
    {
        Console.Write("Enter customer name to search: ");
        var searchTerm = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            Console.WriteLine("Search term cannot be empty.");
            return;
        }

        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = service.SearchCustomersByNamePaged(searchTerm, currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve customers.");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine($"\nNo customers found matching '{searchTerm}'.");
                    return;
                }

                var customers = pagedResult.Items?.ToList() ?? new List<Customer>();
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + customers.Count, pagedResult.TotalCount);

                Console.WriteLine($"\nSearch Results for '{searchTerm}' (Page {currentPage} of {pagedResult.TotalPages})");
                Console.WriteLine($"{"ID",-8} {"Name",-40} {"Email",-30} {"City",-20}");
                Console.WriteLine(new string('-', 100));
                
                foreach (var c in customers)
                {
                    var name = c.FullName?.Length > 38 ? c.FullName.Substring(0, 35) + "..." : c.FullName ?? "N/A";
                    var email = (c.Email ?? "").Length > 28 ? (c.Email ?? "").Substring(0, 25) + "..." : (c.Email ?? "N/A");
                    var city = (c.City ?? "").Length > 18 ? (c.City ?? "").Substring(0, 15) + "..." : (c.City ?? "N/A");
                    
                    Console.WriteLine($"{c.CustomerId,-8} {name,-40} {email,-30} {city,-20}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} customers matching '{searchTerm}'");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  S - Select customer to view details");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "S":
                        Console.Write("Enter customer ID to view details: ");
                        var input = Console.ReadLine();
                        if (int.TryParse(input, out var customerId))
                        {
                            var customer = service.GetCustomerById(customerId);
                            if (customer != null)
                            {
                                Console.WriteLine($"\nID:          {customer.CustomerId}");
                                Console.WriteLine($"Name:        {customer.FullName}");
                                Console.WriteLine($"Email:       {customer.Email}");
                                Console.WriteLine($"Phone:       {customer.Phone}");
                                Console.WriteLine($"AddressLine: {customer.AddressLine}");
                                Console.WriteLine($"City:        {customer.City}");
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Customer ID not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid customer ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                        break;
                }
            }
        }
        catch (SqlException sqlEx)
        {
            Console.WriteLine($"\nERROR: SQL error while searching customers.");
            Console.WriteLine($"Error Number: {sqlEx.Number}");
            Console.WriteLine($"Error Message: {sqlEx.Message}");
            if (sqlEx.Number == -2)
            {
                Console.WriteLine("\nThis appears to be a timeout error. Your database may be very large.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: Failed to search customers.");
            Console.WriteLine($"Error details: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private static void CreateCustomer(ICustomerService service)
    {
        var customer = PromptCustomerInfo(includeId: false);
        var created = service.CreateCustomer(customer);
        Console.WriteLine($"Created customer with ID {created.CustomerId}");
    }

    private static void UpdateCustomer(ICustomerService service)
    {
        Console.Write("Enter customer ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var existing = service.GetCustomerById(id);
        if (existing == null)
        {
            Console.WriteLine("Customer not found.");
            return;
        }

        Console.WriteLine("Leave field empty to keep existing value.");
        var updated = PromptCustomerInfo(includeId: false, existing);
        updated.CustomerId = id;
        service.UpdateCustomer(updated);
        Console.WriteLine("Customer updated.");
    }

    private static void DeleteCustomer(ICustomerService service)
    {
        Console.Write("Enter customer ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        try
        {
        service.DeleteCustomer(id);
            Console.WriteLine("Customer deleted successfully.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"\n{ex.Message}");
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            if (dbEx.InnerException is SqlException sqlEx && sqlEx.Number == 547)
            {
                Console.WriteLine("\nCannot delete customer: This customer has related records (orders or reviews) that prevent deletion.");
                Console.WriteLine("Please delete or reassign the related records first.");
            }
            else
            {
                Console.WriteLine($"\nError deleting customer: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($"Details: {dbEx.InnerException.Message}");
                }
            }
        }
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number == 50000)
            {
                Console.WriteLine($"\n{sqlEx.Message}");
            }
            else if (sqlEx.Number == 547)
            {
                Console.WriteLine("\nCannot delete customer: This customer has related records (orders or reviews) that prevent deletion.");
                Console.WriteLine("Please delete or reassign the related records first.");
            }
            else
            {
                Console.WriteLine($"\nError deleting customer: {sqlEx.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError deleting customer: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Details: {ex.InnerException.Message}");
            }
        }
    }

    private static Customer PromptCustomerInfo(bool includeId, Customer? existing = null)
    {
        var customer = new Customer();

        if (includeId)
        {
            Console.Write("Customer ID: ");
            int.TryParse(Console.ReadLine(), out var id);
            customer.CustomerId = id;
        }

        Console.Write("Full name [{0}]: ", existing?.FullName ?? "");
        var name = Console.ReadLine();
        customer.FullName = string.IsNullOrWhiteSpace(name) ? existing?.FullName ?? "" : name;

        Console.Write("Email [{0}]: ", existing?.Email ?? "");
        var email = Console.ReadLine();
        customer.Email = string.IsNullOrWhiteSpace(email) ? existing?.Email : email;

        Console.Write("Phone [{0}]: ", existing?.Phone ?? "");
        var phone = Console.ReadLine();
        customer.Phone = string.IsNullOrWhiteSpace(phone) ? existing?.Phone : phone;

        Console.Write("Address line [{0}]: ", existing?.AddressLine ?? "");
        var addr = Console.ReadLine();
        customer.AddressLine = string.IsNullOrWhiteSpace(addr) ? existing?.AddressLine : addr;

        Console.Write("City [{0}]: ", existing?.City ?? "");
        var city = Console.ReadLine();
        customer.City = string.IsNullOrWhiteSpace(city) ? existing?.City : city;

        return customer;
    }
   
    private static void ProductMenu()
    {
        var service = ServiceFactory.CreateProductService();
        bool back = false;

        while (!back)
        {
            Console.WriteLine("\n=== Product Management ===");
            Console.WriteLine("1. List all products");
            Console.WriteLine("2. View product by ID");
            Console.WriteLine("3. Create product");
            Console.WriteLine("4. Update product");
            Console.WriteLine("5. Delete product");
            Console.WriteLine("6. List active products");
            Console.WriteLine("7. Get products by category");
            Console.WriteLine("8. Back to main menu");
            Console.Write("Select an option (1-8): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ListProducts(service);
                    break;
                case "2":
                    ViewProduct(service);
                    break;
                case "3":
                    CreateProduct(service);
                    break;
                case "4":
                    UpdateProduct(service);
                    break;
                case "5":
                    DeleteProduct(service);
                    break;
                case "6":
                    ListActiveProducts(service);
                    break;
                case "7":
                    GetProductsByCategory(service);
                    break;
                case "8":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void ListProducts(IProductService service)
    {
        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = service.GetAllProductsPaged(currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve products.");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine("\nNo products found.");
                    return;
                }

                var products = pagedResult.Items?.ToList() ?? new List<Product>();
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + products.Count, pagedResult.TotalCount);

                Console.WriteLine($"\nProducts (Page {currentPage} of {pagedResult.TotalPages})");
                Console.WriteLine($"{"ID",-8} {"Name",-50} {"SKU",-15} {"Price",-12} {"Active",-10}");
                Console.WriteLine(new string('-', 100));
                
                foreach (var p in products)
                {
                    var name = p.Name?.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name ?? "N/A";
                    var sku = p.SKU?.Length > 13 ? p.SKU.Substring(0, 10) + "..." : p.SKU ?? "N/A";
                    var price = p.Price?.ToString("F2") ?? "N/A";
                    var active = p.IsActive ?? "N/A";
                    
                    Console.WriteLine($"{p.ProductId,-8} {name,-50} {sku,-15} ${price,-11} {active,-10}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} products");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  S - Select product to view details");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "S":
                        Console.Write("Enter product ID to view details: ");
                        var input = Console.ReadLine();
                        if (int.TryParse(input, out var productId))
                        {
                            var product = service.GetProductById(productId);
                            if (product != null)
                            {
                                Console.WriteLine($"\nID:          {product.ProductId}");
                                Console.WriteLine($"Name:        {product.Name}");
                                Console.WriteLine($"SKU:         {product.SKU}");
                                Console.WriteLine($"Price:       ${product.Price}");
                                Console.WriteLine($"Description: {product.Description}");
                                Console.WriteLine($"Active:      {product.IsActive}");
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Product ID not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError listing products: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static void ViewProduct(IProductService service)
    {
        Console.Write("Enter product ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var p = service.GetProductById(id);
        if (p == null)
        {
            Console.WriteLine("Product not found.");
            return;
        }

        Console.WriteLine($"\nID:          {p.ProductId}");
        Console.WriteLine($"Name:        {p.Name}");
        Console.WriteLine($"SKU:         {p.SKU}");
        Console.WriteLine($"Price:       ${p.Price}");
        Console.WriteLine($"Description: {p.Description}");
        Console.WriteLine($"Active:      {p.IsActive}");
    }

    private static void CreateProduct(IProductService service)
    {
        var product = PromptProductInfo(includeId: false);
        var created = service.CreateProduct(product);
        Console.WriteLine($"Created product with ID {created.ProductId} and SKU {created.SKU}");
    }

    private static void UpdateProduct(IProductService service)
    {
        Console.Write("Enter product ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var existing = service.GetProductById(id);
        if (existing == null)
        {
            Console.WriteLine("Product not found.");
            return;
        }

        Console.WriteLine("Leave field empty to keep existing value.");
        var updated = PromptProductInfo(includeId: false, existing);
        updated.ProductId = id;
        service.UpdateProduct(updated);
        Console.WriteLine("Product updated.");
    }

    private static void DeleteProduct(IProductService service)
    {
        Console.Write("Enter product ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        service.DeleteProduct(id);
        Console.WriteLine("Delete request sent.");
    }

    private static void ListActiveProducts(IProductService service)
    {
        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = service.GetActiveProductsPaged(currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve active products. ");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine("\nNo active products found.");
                    return;
                }

                var products = pagedResult.Items?.ToList() ?? new List<Product>();
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + products.Count, pagedResult.TotalCount);

                Console.WriteLine($"\nActive Products (Page {currentPage} of {pagedResult.TotalPages})");
                Console.WriteLine($"{"ID",-8} {"Name",-50} {"SKU",-15} {"Price",-12}");
                Console.WriteLine(new string('-', 90));
                
                foreach (var p in products)
                {
                    var name = p.Name?.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name ?? "N/A";
                    var sku = p.SKU?.Length > 13 ? p.SKU.Substring(0, 10) + "..." : p.SKU ?? "N/A";
                    var price = p.Price?.ToString("F2") ?? "N/A";
                    
                    Console.WriteLine($"{p.ProductId,-8} {name,-50} {sku,-15} ${price,-11}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} active products");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  S - Select product to view details");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "S":
                        Console.Write("Enter product ID to view details: ");
                        var input = Console.ReadLine();
                        if (int.TryParse(input, out var productId))
                        {
                            var product = service.GetProductById(productId);
                            if (product != null)
                            {
                                Console.WriteLine($"\nID:          {product.ProductId}");
                                Console.WriteLine($"Name:        {product.Name}");
                                Console.WriteLine($"SKU:         {product.SKU}");
                                Console.WriteLine($"Price:       ${product.Price}");
                                Console.WriteLine($"Description: {product.Description}");
                                Console.WriteLine($"Active:      {product.IsActive}");
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Product ID not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError listing active products: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static void GetProductsByCategory(IProductService service)
    {
        try
        {
            var categoryService = ServiceFactory.CreateCategoryService();
            var categories = categoryService.GetAllCategories().ToList();
            
            if (categories.Count == 0)
            {
                Console.WriteLine("\nNo categories found in database.");
                return;
            }

            Console.WriteLine("\n--- Available Categories ---");
            Console.WriteLine($"{"ID",-8} {"Name",-50} {"Active",-10}");
            Console.WriteLine(new string('-', 70));
            foreach (var c in categories)
            {
                var name = c.Name?.Length > 48 ? c.Name.Substring(0, 45) + "..." : c.Name ?? "N/A";
                var active = c.IsActive ?? "N/A";
                Console.WriteLine($"{c.CategoryId,-8} {name,-50} {active,-10}");
            }

            Console.Write("\nEnter category ID: ");
            if (!int.TryParse(Console.ReadLine(), out var categoryId))
            {
                Console.WriteLine("Invalid category ID.");
                return;
            }

            var selectedCategory = categoryService.GetCategoryById(categoryId);
            if (selectedCategory == null)
            {
                Console.WriteLine($"Category with ID {categoryId} does not exist.");
                return;
            }

            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = service.GetProductsByCategoryPaged(categoryId, currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve products. ");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine($"\nNo products found in category '{selectedCategory.Name}' (ID: {categoryId}).");
                    return;
                }

                var products = pagedResult.Items?.ToList() ?? new List<Product>();
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + products.Count, pagedResult.TotalCount);

                Console.WriteLine($"\n--- Products in Category: {selectedCategory.Name} (ID: {categoryId}) ---");
                Console.WriteLine($"Products (Page {currentPage} of {pagedResult.TotalPages})");
                Console.WriteLine($"{"ID",-8} {"Name",-50} {"SKU",-15} {"Price",-12}");
                Console.WriteLine(new string('-', 90));
                
                foreach (var p in products)
                {
                    var name = p.Name?.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name ?? "N/A";
                    var sku = p.SKU?.Length > 13 ? p.SKU.Substring(0, 10) + "..." : p.SKU ?? "N/A";
                    var price = p.Price?.ToString("F2") ?? "N/A";
                    
                    Console.WriteLine($"{p.ProductId,-8} {name,-50} {sku,-15} ${price,-11}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} products");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  S - Select product to view details");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "S":
                        Console.Write("Enter product ID to view details: ");
                        var input = Console.ReadLine();
                        if (int.TryParse(input, out var productId))
                        {
                            var product = service.GetProductById(productId);
                            if (product != null)
                            {
                                Console.WriteLine($"\nID:          {product.ProductId}");
                                Console.WriteLine($"Name:        {product.Name}");
                                Console.WriteLine($"SKU:         {product.SKU}");
                                Console.WriteLine($"Price:       ${product.Price}");
                                Console.WriteLine($"Description: {product.Description}");
                                Console.WriteLine($"Active:      {product.IsActive}");
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Product ID not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError getting products by category: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static Product PromptProductInfo(bool includeId, Product? existing = null)
    {
        var product = new Product();

        if (includeId)
        {
            Console.Write("Product ID: ");
            int.TryParse(Console.ReadLine(), out var id);
            product.ProductId = id;
        }

        Console.Write("Name [{0}]: ", existing?.Name ?? "");
        var name = Console.ReadLine();
        product.Name = string.IsNullOrWhiteSpace(name) ? existing?.Name ?? "" : name;

        if (existing == null)
        {
            Console.WriteLine("SKU: (will be auto-generated)");
            product.SKU = null; 
        }
        else
        {
            Console.WriteLine("SKU: {0} (cannot be changed)", existing.SKU ?? "N/A");
            product.SKU = existing.SKU; 
        }

        Console.Write("Price [{0}]: ", existing?.Price?.ToString() ?? "");
        var priceStr = Console.ReadLine();
        if (decimal.TryParse(priceStr, out var price))
        {
            product.Price = price;
        }
        else if (existing?.Price != null)
        {
            product.Price = existing.Price;
        }

        Console.Write("Description [{0}]: ", existing?.Description ?? "");
        var desc = Console.ReadLine();
        product.Description = string.IsNullOrWhiteSpace(desc) ? existing?.Description : desc;

        Console.Write("IsActive (True/False) [{0}]: ", existing?.IsActive ?? "");
        var active = Console.ReadLine();
        product.IsActive = string.IsNullOrWhiteSpace(active) ? existing?.IsActive : active;

        return product;
    }
   
    private static void CategoryMenu()
    {
        var service = ServiceFactory.CreateCategoryService();
        bool back = false;

        while (!back)
        {
            Console.WriteLine("\n=== Category Management ===");
            Console.WriteLine("1. List all categories");
            Console.WriteLine("2. View category by ID");
            Console.WriteLine("3. Create category");
            Console.WriteLine("4. Update category");
            Console.WriteLine("5. Delete category");
            Console.WriteLine("6. List active categories");
            Console.WriteLine("7. Back to main menu");
            Console.Write("Select an option (1-7): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ListCategories(service);
                    break;
                case "2":
                    ViewCategory(service);
                    break;
                case "3":
                    CreateCategory(service);
                    break;
                case "4":
                    UpdateCategory(service);
                    break;
                case "5":
                    DeleteCategory(service);
                    break;
                case "6":
                    ListActiveCategories(service);
                    break;
                case "7":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void ListCategories(ICategoryService service)
    {
        Console.WriteLine("\n--- All Categories ---");
        foreach (var c in service.GetAllCategories())
        {
            Console.WriteLine($"{c.CategoryId}: {c.Name} | Active: {c.IsActive}");
        }
    }

    private static void ViewCategory(ICategoryService service)
    {
        Console.Write("Enter category ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var c = service.GetCategoryById(id);
        if (c == null)
        {
            Console.WriteLine("Category not found.");
            return;
        }

        Console.WriteLine($"\nID:     {c.CategoryId}");
        Console.WriteLine($"Name:   {c.Name}");
        Console.WriteLine($"Active: {c.IsActive}");
    }

    private static void CreateCategory(ICategoryService service)
    {
        var category = PromptCategoryInfo(includeId: false);
        var created = service.CreateCategory(category);
        Console.WriteLine($"Created category with ID {created.CategoryId}");
    }

    private static void UpdateCategory(ICategoryService service)
    {
        Console.Write("Enter category ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var existing = service.GetCategoryById(id);
        if (existing == null)
        {
            Console.WriteLine("Category not found.");
            return;
        }

        Console.WriteLine("Leave field empty to keep existing value.");
        var updated = PromptCategoryInfo(includeId: false, existing);
        updated.CategoryId = id;
        service.UpdateCategory(updated);
        Console.WriteLine("Category updated.");
    }

    private static void DeleteCategory(ICategoryService service)
    {
        Console.Write("Enter category ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var category = service.GetCategoryById(id);
        if (category == null)
        {
            Console.WriteLine($"Category with ID {id} does not exist.");
            return;
        }

        try
        {
            service.DeleteCategory(id);
            Console.WriteLine("Category deleted successfully.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"\nCannot delete category: {ex.Message}");
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            if (dbEx.InnerException is SqlException sqlEx && sqlEx.Number == 547)
            {
                Console.WriteLine("\nCannot delete category: This category is associated with one or more products.");
                Console.WriteLine("Please remove the category from all products first, or delete the products.");
            }
            else
            {
                Console.WriteLine($"\nError deleting category: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($"Details: {dbEx.InnerException.Message}");
                }
            }
        }
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number == 50000) 
            {
                Console.WriteLine($"\n{sqlEx.Message}");
            }
            else if (sqlEx.Number == 547)
            {
                Console.WriteLine("\nCannot delete category: This category is associated with one or more products.");
                Console.WriteLine("Please remove the category from all products first, or delete the products.");
            }
            else
            {
                Console.WriteLine($"\nError deleting category: {sqlEx.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError deleting category: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Details: {ex.InnerException.Message}");
            }
        }
    }

    private static void ListActiveCategories(ICategoryService service)
    {
        Console.WriteLine("\n--- Active Categories ---");
        foreach (var c in service.GetActiveCategories())
        {
            Console.WriteLine($"{c.CategoryId}: {c.Name}");
        }
    }

    private static Category PromptCategoryInfo(bool includeId, Category? existing = null)
    {
        var category = new Category();

        if (includeId)
        {
            Console.Write("Category ID: ");
            int.TryParse(Console.ReadLine(), out var id);
            category.CategoryId = id;
        }

        Console.Write("Name [{0}]: ", existing?.Name ?? "");
        var name = Console.ReadLine();
        category.Name = string.IsNullOrWhiteSpace(name) ? existing?.Name ?? "" : name;

        Console.Write("IsActive (True/False) [{0}]: ", existing?.IsActive ?? "");
        var active = Console.ReadLine();
        category.IsActive = string.IsNullOrWhiteSpace(active) ? existing?.IsActive : active;

        return category;
    }
  
    private static void OrderMenu()
    {
        var service = ServiceFactory.CreateOrderService();
        bool back = false;

        while (!back)
        {
            Console.WriteLine("\n=== Order Management ===");
            Console.WriteLine("1. List all orders");
            Console.WriteLine("2. View order by ID");
            Console.WriteLine("3. Create order");
            Console.WriteLine("4. Update order status");
            Console.WriteLine("5. Cancel order");
            Console.WriteLine("6. Get orders by customer");
            Console.WriteLine("7. View order items");
            Console.WriteLine("8. Add item to order");
            Console.WriteLine("9. Back to main menu");
            Console.Write("Select an option (1-9): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ListOrders(service);
                    break;
                case "2":
                    ViewOrder(service);
                    break;
                case "3":
                    CreateOrder(service);
                    break;
                case "4":
                    UpdateOrderStatus(service);
                    break;
                case "5":
                    CancelOrder(service);
                    break;
                case "6":
                    GetOrdersByCustomer(service);
                    break;
                case "7":
                    ViewOrderItems(service);
                    break;
                case "8":
                    AddOrderItem(service);
                    break;
                case "9":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void ListOrders(IOrderService service)
    {
        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            PagedResult<OrderSummary>? pagedResult = null;

            while (true)
            {
                pagedResult = service.GetAllOrderSummariesPaged(currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve orders. ");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine("\nNo orders found.");
                    return;
                }

                var orderSummaries = pagedResult.Items?.ToList() ?? new List<OrderSummary>();
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + orderSummaries.Count, pagedResult.TotalCount);

                Console.WriteLine($"\nOrders (Page {currentPage} of {pagedResult.TotalPages})");
                Console.WriteLine($"Total Orders: {pagedResult.TotalCount}");
                Console.WriteLine($"{"Order ID",-12} {"Customer Name",-30} {"Status",-15} {"Total",-15} {"Date",-20}");
                Console.WriteLine(new string('-', 95));
                
                foreach (var os in orderSummaries)
                {
                    var total = os.CalculatedTotalAmount.ToString("F2");
                    var date = os.OrderDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
                    var status = os.Status ?? "N/A";
                    var statusDisplay = status.Length > 13 ? status.Substring(0, 10) + "..." : status;
                    var customerName = os.CustomerName.Length > 28 ? os.CustomerName.Substring(0, 25) + "..." : os.CustomerName;
                    
                    Console.WriteLine($"{os.OrderId,-12} {customerName,-30} {statusDisplay,-15} ${total,-14} {date,-20}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} orders");
                
                if (pagedResult.TotalPages <= 1)
                {
                    Console.WriteLine("\nOptions:");
                    Console.WriteLine("  S - Select order to view details");
                    Console.WriteLine("  Q - Quit (go back)");
                    Console.Write("\nEnter option: ");

                    var option = Console.ReadLine()?.Trim().ToUpper();

                    switch (option)
                    {
                        case "S":
                            Console.Write("Enter order ID to view details: ");
                            var input = Console.ReadLine();
                            if (int.TryParse(input, out var orderId))
                            {
                                ViewOrderDetails(service, orderId);
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Invalid order ID. Please enter a number.");
                            }
                            break;
                        case "Q":
                            return;
                        default:
                            Console.WriteLine("Invalid option. Please enter S or Q.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("\nOptions:");
                    Console.WriteLine("  N - Next page");
                    Console.WriteLine("  P - Previous page");
                    Console.WriteLine("  S - Select order to view details");
                    Console.WriteLine("  Q - Quit (go back)");
                    Console.Write("\nEnter option: ");

                    var option = Console.ReadLine()?.Trim().ToUpper();

                    switch (option)
                    {
                        case "N":
                            if (currentPage < pagedResult.TotalPages)
                            {
                                currentPage++;
                            }
                            else
                            {
                                Console.WriteLine("Already on the last page.");
                            }
                            break;

                        case "P":
                            if (currentPage > 1)
                            {
                                currentPage--;
                            }
                            else
                            {
                                Console.WriteLine("Already on the first page.");
                            }
                            break;

                        case "S":
                            Console.Write("Enter order ID to view details: ");
                            var input = Console.ReadLine();
                            if (int.TryParse(input, out var orderId))
                            {
                                ViewOrderDetails(service, orderId);
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Invalid order ID. Please enter a number.");
                            }
                            break;

                        case "Q":
                            return;

                        default:
                            Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }

    private static void ViewOrder(IOrderService service)
    {
        Console.Write("Enter order ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        ViewOrderDetails(service, id);
    }

    private static void ViewOrderDetails(IOrderService service, int orderId)
    {
        var orderSummary = service.GetOrderSummaryById(orderId);
        if (orderSummary == null)
        {
            Console.WriteLine("Order not found.");
            return;
        }

        var storedTotal = orderSummary.StoredTotalAmount?.ToString("F2") ?? "0.00";
        var calculatedTotal = orderSummary.CalculatedTotalAmount.ToString("F2");
        var date = orderSummary.OrderDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";

        Console.WriteLine($"\n=== Order Details ===");
        Console.WriteLine($"Order ID:              {orderSummary.OrderId}");
        Console.WriteLine($"Date:                  {date}");
        Console.WriteLine($"Status:                {orderSummary.Status}");
        Console.WriteLine($"Stored Total:          ${storedTotal}");
        Console.WriteLine($"Calculated Total:      ${calculatedTotal}");
        if (orderSummary.StoredTotalAmount != orderSummary.CalculatedTotalAmount)
        {
            Console.WriteLine($"Warning: Stored total differs from calculated total!");
        }
        Console.WriteLine($"\nCustomer Information:");
        Console.WriteLine($"  Customer ID:         {orderSummary.CustomerId}");
        Console.WriteLine($"  Customer Name:       {orderSummary.CustomerName}");
        Console.WriteLine($"  Customer Email:      {orderSummary.CustomerEmail ?? "N/A"}");
        Console.WriteLine($"\nAdmin Information:");
        Console.WriteLine($"  Admin ID:            {orderSummary.AdminUserId?.ToString() ?? "N/A"}");
        Console.WriteLine($"  Admin Email:         {orderSummary.AdminEmail ?? "N/A"}");

        var orderItems = service.GetOrderItems(orderId).ToList();
        if (orderItems.Count > 0)
        {
            Console.WriteLine($"\n--- Order Items ({orderItems.Count} item(s)) ---");
            var productService = ServiceFactory.CreateProductService();
            
            foreach (var item in orderItems)
            {
                var product = productService.GetProductById(item.ProductId);
                var productName = product?.Name ?? $"Product {item.ProductId}";
                Console.WriteLine($"  • {productName}");
                Console.WriteLine($"    Product ID: {item.ProductId} | Quantity: {item.Quantity} | Unit Price: ${item.UnitPriceAtOrder:F2} | Line Total: ${item.LineTotalAmount:F2}");
            }
        }
        else
        {
            Console.WriteLine("\n--- No items in this order ---");
        }
    }

    private static void CreateOrder(IOrderService service)
    {
        if (_currentAdmin == null)
        {
            Console.WriteLine("Error: Admin must be logged in to create orders.");
            return;
        }

        var customerService = ServiceFactory.CreateCustomerService();
        var customers = customerService.GetAllCustomers().ToList();
        
        if (customers.Count == 0)
        {
            Console.WriteLine("No customers found in database.");
            return;
        }

        int customerId = 0;
        const int pageSize = 50;
        int currentPage = 1;
        int totalPages = (int)Math.Ceiling(customers.Count / (double)pageSize);

        Console.WriteLine("\n=== Select Customer ===");
        while (customerId == 0)
        {
            int startIndex = (currentPage - 1) * pageSize;
            int endIndex = Math.Min(startIndex + pageSize, customers.Count);
            var pageCustomers = customers.Skip(startIndex).Take(pageSize).ToList();

            Console.WriteLine($"\nCustomers (Page {currentPage} of {totalPages})");
            Console.WriteLine($"{"ID",-8} {"Name",-40} {"Email",-30} {"City",-20}");
            Console.WriteLine(new string('-', 100));
            
            foreach (var c in pageCustomers)
            {
                var name = c.FullName.Length > 38 ? c.FullName.Substring(0, 35) + "..." : c.FullName;
                var email = (c.Email ?? "").Length > 28 ? (c.Email ?? "").Substring(0, 25) + "..." : (c.Email ?? "");
                var city = (c.City ?? "").Length > 18 ? (c.City ?? "").Substring(0, 15) + "..." : (c.City ?? "");
                Console.WriteLine($"{c.CustomerId,-8} {name,-40} {email,-30} {city,-20}");
            }

            Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {customers.Count} customers");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  N - Next page");
            Console.WriteLine("  P - Previous page");
            Console.WriteLine("  S - Select by ID");
            Console.WriteLine("  Q - Quit (cancel)");
            Console.Write("\nEnter option: ");

            var option = Console.ReadLine()?.Trim().ToUpper();

            switch (option)
            {
                case "N":
                    if (currentPage < totalPages)
                    {
                        currentPage++;
                    }
                    else
                    {
                        Console.WriteLine("Already on the last page.");
                    }
                    break;

                case "P":
                    if (currentPage > 1)
                    {
                        currentPage--;
                    }
                    else
                    {
                        Console.WriteLine("Already on the first page.");
                    }
                    break;

                case "S":
                    Console.Write("Enter customer ID: ");
                    var input = Console.ReadLine();
                    if (int.TryParse(input, out var selectedId))
                    {
                        if (customers.Any(c => c.CustomerId == selectedId))
                        {
                            customerId = selectedId;
                        }
                        else
                        {
                            Console.WriteLine("Customer ID not found. Please try again.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid customer ID. Please enter a number.");
                    }
                    break;

                case "Q":
                    Console.WriteLine("Cancelled.");
                    return;

                default:
                    Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                    break;
            }
        }

        var selectedCustomer = customerService.GetCustomerById(customerId);
        Console.WriteLine($"\nSelected Customer: {selectedCustomer?.FullName} (ID: {customerId})");

        var order = service.CreateOrder(customerId, _currentAdmin.AdminUserId, "Pending");
        Console.WriteLine($"\nCreated order with ID {order.OrderId}");

        var productService = ServiceFactory.CreateProductService();
        var inventoryService = ServiceFactory.CreateInventoryService();
        bool addingItems = true;

        Console.WriteLine("\n=== Add Products to Order ===");
        while (addingItems)
        {
            var products = productService.GetAllProducts().ToList();
            
            if (products.Count == 0)
            {
                Console.WriteLine("No products available in database.");
                break;
            }

            int productId = 0;
            currentPage = 1;
            totalPages = (int)Math.Ceiling(products.Count / (double)pageSize);

            while (productId == 0)
            {
                int startIndex = (currentPage - 1) * pageSize;
                int endIndex = Math.Min(startIndex + pageSize, products.Count);
                var pageProducts = products.Skip(startIndex).Take(pageSize).ToList();

                Console.WriteLine($"\nProducts (Page {currentPage} of {totalPages})");
                Console.WriteLine($"{"ID",-8} {"Name",-50}");
                Console.WriteLine(new string('-', 60));
                
                foreach (var p in pageProducts)
                {
                    var name = p.Name.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name;
                    Console.WriteLine($"{p.ProductId,-8} {name,-50}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {products.Count} products");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  S - Select by ID");
                Console.WriteLine("  D - Done (finish adding items)");
                Console.Write("\nEnter option: ");

                var productOption = Console.ReadLine()?.Trim().ToUpper();

                switch (productOption)
                {
                    case "N":
                        if (currentPage < totalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "S":
                        Console.Write("Enter product ID: ");
                        var productInput = Console.ReadLine();
                        if (int.TryParse(productInput, out var selectedProductId))
                        {
                            if (products.Any(p => p.ProductId == selectedProductId))
                            {
                                productId = selectedProductId;
                            }
                            else
                            {
                                Console.WriteLine("Product ID not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "D":
                        addingItems = false;
                        productId = -1;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, S, or D.");
                        break;
                }
            }

            if (productId <= 0)
            {
                break; 
            }

            var selectedProduct = productService.GetProductById(productId);
            if (selectedProduct == null)
            {
                Console.WriteLine("Product not found.");
                continue;
            }

            var stock = inventoryService.GetStockQuantity(productId);
            Console.WriteLine($"\nSelected Product: {selectedProduct.Name}");
            Console.WriteLine($"Current Price: ${selectedProduct.Price?.ToString("F2") ?? "N/A"}");
            Console.WriteLine($"Stock Available: {stock}");

            Console.Write("Enter quantity: ");
            if (!int.TryParse(Console.ReadLine(), out var quantity) || quantity <= 0)
            {
                Console.WriteLine("Invalid quantity. Must be greater than 0.");
                continue;
            }

            if (quantity > stock)
            {
                Console.WriteLine($"Warning: Requested quantity ({quantity}) exceeds available stock ({stock}).");
                Console.Write("Continue anyway? (y/n): ");
                var confirm = Console.ReadLine();
                if (confirm?.ToLower() != "y")
                {
                    continue;
                }
            }

            var unitPrice = selectedProduct.Price ?? 0;
            Console.WriteLine($"Using product price: ${unitPrice:F2}");

            Console.Write("\nStatus (default: Pending): ");
            var statusInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(statusInput))
            {
                service.UpdateOrderStatus(order.OrderId, statusInput);
            }

            try
            {
                service.AddOrderItem(order.OrderId, productId, quantity, unitPrice);
                Console.WriteLine($"Added {quantity} x {selectedProduct.Name} to order");
                
                var updatedOrder = service.GetOrderById(order.OrderId);
                Console.WriteLine($"Order total is now: ${updatedOrder?.TotalAmount?.ToString("F2") ?? "0.00"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding order item: {ex.Message}");
            }

            Console.Write("\nAdd another item? (y/n): ");
            var response = Console.ReadLine();
            if (response?.ToLower() != "y")
            {
                addingItems = false;
            }
        }

        var finalOrder = service.GetOrderById(order.OrderId);
        Console.WriteLine($"\n=== Order Created Successfully ===");
        Console.WriteLine($"Order ID: {finalOrder?.OrderId}");
        Console.WriteLine($"Customer: {selectedCustomer?.FullName} (ID: {customerId})");
        Console.WriteLine($"Status: {finalOrder?.Status}");
        Console.WriteLine($"Total: ${finalOrder?.TotalAmount?.ToString("F2") ?? "0.00"}");
        
        var orderItems = service.GetOrderItems(order.OrderId).ToList();
        if (orderItems.Count > 0)
        {
            Console.WriteLine($"\nOrder Items ({orderItems.Count} item(s)):");
            foreach (var item in orderItems)
            {
                var itemProduct = productService.GetProductById(item.ProductId);
                Console.WriteLine($"  • {itemProduct?.Name}: {item.Quantity} x ${item.UnitPriceAtOrder:F2} = ${item.LineTotalAmount:F2}");
            }
        }
    }

    private static void UpdateOrderStatus(IOrderService service)
    {
        Console.Write("Enter order ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("Enter new status: ");
        var status = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(status))
        {
            Console.WriteLine("Status cannot be empty.");
            return;
        }

        service.UpdateOrderStatus(id, status);
        Console.WriteLine("Order status updated.");
    }

    private static void CancelOrder(IOrderService service)
    {
        Console.Write("Enter order ID to cancel: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        service.CancelOrder(id);
        Console.WriteLine("Order cancelled.");
    }

    private static void GetOrdersByCustomer(IOrderService service)
    {
        var customerService = ServiceFactory.CreateCustomerService();
        var customers = customerService.GetAllCustomers().ToList();
        
        if (customers.Count == 0)
        {
            Console.WriteLine("No customers found in database.");
            return;
        }

        int customerId = 0;
        const int pageSize = 50;
        int currentPage = 1;
        int totalPages = (int)Math.Ceiling(customers.Count / (double)pageSize);

        Console.WriteLine("\n=== Select Customer ===");
        while (customerId == 0)
        {
            int startIndex = (currentPage - 1) * pageSize;
            int endIndex = Math.Min(startIndex + pageSize, customers.Count);
            var pageCustomers = customers.Skip(startIndex).Take(pageSize).ToList();

            Console.WriteLine($"\nCustomers (Page {currentPage} of {totalPages})");
            Console.WriteLine($"{"ID",-8} {"Name",-40} {"Email",-30} {"City",-20}");
            Console.WriteLine(new string('-', 100));
            
            foreach (var c in pageCustomers)
            {
                var name = c.FullName.Length > 38 ? c.FullName.Substring(0, 35) + "..." : c.FullName;
                var email = (c.Email ?? "").Length > 28 ? (c.Email ?? "").Substring(0, 25) + "..." : (c.Email ?? "");
                var city = (c.City ?? "").Length > 18 ? (c.City ?? "").Substring(0, 15) + "..." : (c.City ?? "");
                Console.WriteLine($"{c.CustomerId,-8} {name,-40} {email,-30} {city,-20}");
            }

            Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {customers.Count} customers");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  N - Next page");
            Console.WriteLine("  P - Previous page");
            Console.WriteLine("  S - Select by ID");
            Console.WriteLine("  Q - Quit (cancel)");
            Console.Write("\nEnter option: ");

            var option = Console.ReadLine()?.Trim().ToUpper();

            switch (option)
            {
                case "N":
                    if (currentPage < totalPages)
                    {
                        currentPage++;
                    }
                    else
                    {
                        Console.WriteLine("Already on the last page.");
                    }
                    break;

                case "P":
                    if (currentPage > 1)
                    {
                        currentPage--;
                    }
                    else
                    {
                        Console.WriteLine("Already on the first page.");
                    }
                    break;

                case "S":
                    Console.Write("Enter customer ID: ");
                    var input = Console.ReadLine();
                    if (int.TryParse(input, out var selectedId))
                    {
                        if (customers.Any(c => c.CustomerId == selectedId))
                        {
                            customerId = selectedId;
                        }
                        else
                        {
                            Console.WriteLine("Customer ID not found. Please try again.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid customer ID. Please enter a number.");
                    }
                    break;

                case "Q":
                    Console.WriteLine("Cancelled.");
                    return;

                default:
                    Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                    break;
            }
        }

        var selectedCustomer = customerService.GetCustomerById(customerId);
        Console.WriteLine($"\n=== Orders for Customer: {selectedCustomer?.FullName} (ID: {customerId}) ===");
        
        var orders = service.GetOrdersByCustomer(customerId).ToList();
        
        if (orders.Count == 0)
        {
            Console.WriteLine($"\nNo orders found for this customer.");
            return;
        }

        Console.WriteLine($"\nFound {orders.Count} order(s):");
        foreach (var o in orders)
        {
            var total = o.TotalAmount?.ToString("F2") ?? "0.00";
            var date = o.OrderDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
            Console.WriteLine($"Order {o.OrderId}: Status: {o.Status} | Total: ${total} | Date: {date}");
        }
        
        Console.Write("\nEnter order ID to view details (or 0 to go back): ");
        if (int.TryParse(Console.ReadLine(), out var orderId) && orderId > 0)
        {
            ViewOrderDetails(service, orderId);
        }
    }

    private static void ViewOrderItems(IOrderService service)
    {
        Console.Write("Enter order ID: ");
        if (!int.TryParse(Console.ReadLine(), out var orderId))
        {
            Console.WriteLine("Invalid order ID.");
            return;
        }

        var orderItems = service.GetOrderItems(orderId).ToList();
        
        if (orderItems.Count == 0)
        {
            Console.WriteLine($"\nNo items found in order {orderId}.");
            return;
        }

        Console.WriteLine($"\n--- Items in Order {orderId} ({orderItems.Count} item(s)) ---");
        var productService = ServiceFactory.CreateProductService();
        
        decimal grandTotal = 0;
        foreach (var item in orderItems)
        {
            var product = productService.GetProductById(item.ProductId);
            var productName = product?.Name ?? $"Product {item.ProductId}";
            var lineTotal = item.LineTotalAmount;
            grandTotal += lineTotal;
            
            Console.WriteLine($"\nItem {item.OrderItemId}:");
            Console.WriteLine($"  Product: {productName} (ID: {item.ProductId})");
            Console.WriteLine($"  Quantity: {item.Quantity}");
            Console.WriteLine($"  Unit Price: ${item.UnitPriceAtOrder:F2}");
            Console.WriteLine($"  Line Total: ${lineTotal:F2}");
        }
        
        Console.WriteLine($"\n--- Grand Total: ${grandTotal:F2} ---");
    }

    private static void AddOrderItem(IOrderService service)
    {
        Console.Write("Enter order ID: ");
        if (!int.TryParse(Console.ReadLine(), out var orderId))
        {
            Console.WriteLine("Invalid order ID.");
            return;
        }

        var order = service.GetOrderById(orderId);
        if (order == null)
        {
            Console.WriteLine("Order not found.");
            return;
        }

        var productService = ServiceFactory.CreateProductService();
        var products = productService.GetAllProducts().ToList();
        
        if (products.Count == 0)
        {
            Console.WriteLine("No products available in database.");
            return;
        }

        int productId = 0;
        const int pageSize = 50;
        int currentPage = 1;
        int totalPages = (int)Math.Ceiling(products.Count / (double)pageSize);

        while (productId == 0)
        {
            int startIndex = (currentPage - 1) * pageSize;
            int endIndex = Math.Min(startIndex + pageSize, products.Count);
            var pageProducts = products.Skip(startIndex).Take(pageSize).ToList();

            Console.WriteLine($"\nProducts (Page {currentPage} of {totalPages})");
            Console.WriteLine($"{"ID",-8} {"Name",-50}");
            Console.WriteLine(new string('-', 60));
            
            foreach (var p in pageProducts)
            {
                var name = p.Name.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name;
                Console.WriteLine($"{p.ProductId,-8} {name,-50}");
            }

            Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {products.Count} products");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  N - Next page");
            Console.WriteLine("  P - Previous page");
            Console.WriteLine("  S - Select by ID");
            Console.WriteLine("  Q - Quit (cancel)");
            Console.Write("\nEnter option: ");

            var option = Console.ReadLine()?.Trim().ToUpper();

            switch (option)
            {
                case "N":
                    if (currentPage < totalPages)
                    {
                        currentPage++;
                    }
                    else
                    {
                        Console.WriteLine("Already on the last page.");
                    }
                    break;

                case "P":
                    if (currentPage > 1)
                    {
                        currentPage--;
                    }
                    else
                    {
                        Console.WriteLine("Already on the first page.");
                    }
                    break;

                case "S":
                    Console.Write("Enter product ID: ");
                    var input = Console.ReadLine();
                    if (int.TryParse(input, out var selectedId))
                    {
                        if (products.Any(p => p.ProductId == selectedId))
                        {
                            productId = selectedId;
                        }
                        else
                        {
                            Console.WriteLine("Product ID not found. Please try again.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid product ID. Please enter a number.");
                    }
                    break;

                case "Q":
                    Console.WriteLine("Cancelled.");
                    return;

                default:
                    Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                    break;
            }
        }

        var selectedProduct = productService.GetProductById(productId);
        if (selectedProduct == null)
        {
            Console.WriteLine("Product not found.");
            return;
        }

        var inventoryService = ServiceFactory.CreateInventoryService();
        var stock = inventoryService.GetStockQuantity(productId);
        Console.WriteLine($"\nSelected Product: {selectedProduct.Name}");
        Console.WriteLine($"Current Price: ${selectedProduct.Price?.ToString("F2") ?? "N/A"}");
        Console.WriteLine($"Stock Available: {stock}");

        Console.Write("Enter quantity: ");
        if (!int.TryParse(Console.ReadLine(), out var quantity) || quantity <= 0)
        {
            Console.WriteLine("Invalid quantity. Must be greater than 0.");
            return;
        }

        if (quantity > stock)
        {
            Console.WriteLine($"Warning: Requested quantity ({quantity}) exceeds available stock ({stock}).");
            Console.Write("Continue anyway? (y/n): ");
            var confirm = Console.ReadLine();
            if (confirm?.ToLower() != "y")
            {
                Console.WriteLine("Cancelled.");
                return;
            }
        }

        var unitPrice = selectedProduct.Price ?? 0;
        Console.WriteLine($"Using product price: ${unitPrice:F2}");

        Console.Write("\nStatus (default: Pending): ");
        var statusInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(statusInput))
        {
            service.UpdateOrderStatus(orderId, statusInput);
        }

        var lineTotal = quantity * unitPrice;
        Console.WriteLine($"\n--- Order Item Summary ---");
        Console.WriteLine($"Product: {selectedProduct.Name} (ID: {productId})");
        Console.WriteLine($"Quantity: {quantity}");
        Console.WriteLine($"Unit Price: ${unitPrice:F2}");
        Console.WriteLine($"Line Total: ${lineTotal:F2}");
        Console.Write("\nAdd this item to order? (y/n): ");
        var finalConfirm = Console.ReadLine();

        if (finalConfirm?.ToLower() == "y")
        {
            try
            {
                service.AddOrderItem(orderId, productId, quantity, unitPrice);
                Console.WriteLine("Order item added successfully!");
     
                var updatedOrder = service.GetOrderById(orderId);
                Console.WriteLine($"Order total is now: ${updatedOrder?.TotalAmount?.ToString("F2") ?? "0.00"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding order item: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Cancelled.");
        }
    }
   
    private static void ReviewMenu()
    {
        var service = ServiceFactory.CreateReviewService();
        bool back = false;

        while (!back)
        {
            Console.WriteLine("\n=== Review Management ===");
            Console.WriteLine("1. List all reviews");
            Console.WriteLine("2. View review by ID");
            Console.WriteLine("3. Delete review");
            Console.WriteLine("4. Get reviews by product");
            Console.WriteLine("5. Get approved reviews for product");
            Console.WriteLine("6. Approve/Reject review");
            Console.WriteLine("7. Back to main menu");
            Console.Write("Select an option (1-7): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ListReviews(service);
                    break;
                case "2":
                    ViewReview(service);
                    break;
                case "3":
                    DeleteReview(service);
                    break;
                case "4":
                    GetReviewsByProduct(service);
                    break;
                case "5":
                    GetApprovedReviews(service);
                    break;
                case "6":
                    ApproveReview(service);
                    break;
                case "7":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void ListReviews(IReviewService service)
    {
        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = service.GetAllReviewsPaged(currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve reviews. ");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine("\nNo reviews found.");
                    return;
                }

                var reviews = pagedResult.Items?.ToList() ?? new List<Review>();
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + reviews.Count, pagedResult.TotalCount);

                Console.WriteLine($"\nReviews (Page {currentPage} of {pagedResult.TotalPages})");
                Console.WriteLine($"{"Review ID",-12} {"Product ID",-12} {"Customer ID",-13} {"Rating",-8} {"Approved",-10}");
                Console.WriteLine(new string('-', 60));
                
                foreach (var r in reviews)
                {
                    var rating = r.Rating.ToString();
                    var approved = r.IsApproved ?? "N/A";
                    
                    Console.WriteLine($"{r.ReviewId,-12} {r.ProductId,-12} {r.CustomerId,-13} {rating,-8} {approved,-10}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} reviews");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  S - Select review to view details");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "S":
                        Console.Write("Enter review ID to view details: ");
                        var input = Console.ReadLine();
                        if (int.TryParse(input, out var reviewId))
                        {
                            var review = service.GetReviewById(reviewId);
                            if (review != null)
                            {
                                Console.WriteLine($"\nReview ID:   {review.ReviewId}");
                                Console.WriteLine($"Product ID:  {review.ProductId}");
                                Console.WriteLine($"Customer ID: {review.CustomerId}");
                                Console.WriteLine($"Rating:      {review.Rating}/5");
                                Console.WriteLine($"Comments:    {review.Comments}");
                                Console.WriteLine($"Approved:    {review.IsApproved}");
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Review ID not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid review ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError listing reviews: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static void ViewReview(IReviewService service)
    {
        Console.Write("Enter review ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var r = service.GetReviewById(id);
        if (r == null)
        {
            Console.WriteLine("Review not found.");
            return;
        }

        Console.WriteLine($"\nReview ID:    {r.ReviewId}");
        Console.WriteLine($"Product ID:   {r.ProductId}");
        Console.WriteLine($"Customer ID: {r.CustomerId}");
        Console.WriteLine($"Rating:       {r.Rating}/5");
        Console.WriteLine($"Comments:     {r.Comments}");
        Console.WriteLine($"Approved:     {r.IsApproved}");
    }

    private static void CreateReview(IReviewService service)
    {
        var review = PromptReviewInfo(includeId: false);
        var created = service.CreateReview(review);
        Console.WriteLine($"Created review with ID {created.ReviewId}");
    }

    private static void UpdateReview(IReviewService service)
    {
        Console.Write("Enter review ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var existing = service.GetReviewById(id);
        if (existing == null)
        {
            Console.WriteLine("Review not found.");
            return;
        }

        Console.WriteLine("Leave field empty to keep existing value.");
        var updated = PromptReviewInfo(includeId: false, existing);
        updated.ReviewId = id;
        updated.ProductId = existing.ProductId;
        updated.CustomerId = existing.CustomerId;
        service.UpdateReview(updated);
        Console.WriteLine("Review updated.");
    }

    private static void DeleteReview(IReviewService service)
    {
        Console.Write("Enter review ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        service.DeleteReview(id);
        Console.WriteLine("Review deleted.");
    }

    private static void GetReviewsByProduct(IReviewService service)
    {
        Console.Write("Enter product ID: ");
        if (!int.TryParse(Console.ReadLine(), out var productId))
        {
            Console.WriteLine("Invalid product ID.");
            return;
        }

        Console.WriteLine($"\n--- Reviews for Product {productId} ---");
        foreach (var r in service.GetReviewsByProduct(productId))
        {
            var comments = string.IsNullOrWhiteSpace(r.Comments) ? "No comments" : r.Comments;
            Console.WriteLine($"Review {r.ReviewId}: Customer {r.CustomerId} | Rating: {r.Rating}/5 | Approved: {r.IsApproved}");
            Console.WriteLine($"  Comments: {comments}");
        }
    }

    private static void GetApprovedReviews(IReviewService service)
    {
        Console.Write("Enter product ID: ");
        if (!int.TryParse(Console.ReadLine(), out var productId))
        {
            Console.WriteLine("Invalid product ID.");
            return;
        }

        Console.WriteLine($"\n--- Approved Reviews for Product {productId} ---");
        foreach (var r in service.GetApprovedReviews(productId))
        {
            Console.WriteLine($"Review {r.ReviewId}: Customer {r.CustomerId} | Rating: {r.Rating}/5 | Comments: {r.Comments}");
        }
    }

    private static void ApproveReview(IReviewService service)
    {
        Console.Write("Enter review ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var review = service.GetReviewById(id);
        if (review == null)
        {
            Console.WriteLine("Review not found.");
            return;
        }

        Console.WriteLine($"\nCurrent status: {(review.IsApproved == "True" ? "Approved" : review.IsApproved == "False" ? "Rejected" : "Pending")}");
        Console.WriteLine("1. Approve");
        Console.WriteLine("2. Reject");
        Console.Write("Select an option (1-2): ");

        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                service.ApproveReview(id);
                Console.WriteLine("Review approved.");
                break;
            case "2":
                service.RejectReview(id);
                Console.WriteLine("Review rejected.");
                break;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }
    }

    private static Review PromptReviewInfo(bool includeId, Review? existing = null)
    {
        var review = new Review();

        if (includeId)
        {
            Console.Write("Review ID: ");
            int.TryParse(Console.ReadLine(), out var id);
            review.ReviewId = id;
        }

        if (existing == null)
        {
            Console.Write("Product ID: ");
            int.TryParse(Console.ReadLine(), out var productId);
            review.ProductId = productId;

            Console.Write("Customer ID: ");
            int.TryParse(Console.ReadLine(), out var customerId);
            review.CustomerId = customerId;
        }

        Console.Write("Rating (1-5) [{0}]: ", existing?.Rating.ToString() ?? "");
        var ratingStr = Console.ReadLine();
        if (int.TryParse(ratingStr, out var rating) && rating >= 1 && rating <= 5)
        {
            review.Rating = rating;
        }
        else if (existing != null)
        {
            review.Rating = existing.Rating;
        }

        Console.Write("Comments [{0}]: ", existing?.Comments ?? "");
        var comments = Console.ReadLine();
        review.Comments = string.IsNullOrWhiteSpace(comments) ? existing?.Comments : comments;

        Console.Write("IsApproved (True/False) [{0}]: ", existing?.IsApproved ?? "");
        var approved = Console.ReadLine();
        review.IsApproved = string.IsNullOrWhiteSpace(approved) ? existing?.IsApproved : approved;

        return review;
    }
  
    private static void InventoryMenu()
    {
        var service = ServiceFactory.CreateInventoryService();
        bool back = false;

        while (!back)
        {
            Console.WriteLine("\n=== Inventory Management ===");
            Console.WriteLine("1. View inventory by product ID");
            Console.WriteLine("2. Update inventory quantity");
            Console.WriteLine("3. Get stock quantity");
            Console.WriteLine("4. Get low stock products");
            Console.WriteLine("5. Back to main menu");
            Console.Write("Select an option (1-5): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ViewInventory(service);
                    break;
                case "2":
                    UpdateInventory(service);
                    break;
                case "3":
                    GetStockQuantity(service);
                    break;
                case "4":
                    GetLowStockProducts(service);
                    break;
                case "5":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void ViewInventory(IInventoryService inventoryService)
    {
        var productService = ServiceFactory.CreateProductService();
        
        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = productService.GetAllProductsPaged(currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve products.");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine("\nNo products found.");
                    return;
                }

                var products = pagedResult.Items?.ToList() ?? new List<Product>();
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + products.Count, pagedResult.TotalCount);

                Console.WriteLine($"\nInventory (Page {currentPage} of {pagedResult.TotalPages})");
                Console.WriteLine($"{"ID",-8} {"Name",-50} {"SKU",-15} {"Price",-12} {"Stock",-10}");
                Console.WriteLine(new string('-', 100));
                
                foreach (var p in products)
                {
                    var name = p.Name?.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name ?? "N/A";
                    var sku = p.SKU?.Length > 13 ? p.SKU.Substring(0, 10) + "..." : p.SKU ?? "N/A";
                    var price = p.Price?.ToString("F2") ?? "N/A";
                    var stock = inventoryService.GetStockQuantity(p.ProductId);
                    
                    Console.WriteLine($"{p.ProductId,-8} {name,-50} {sku,-15} ${price,-11} {stock,-10}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} products");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  S - Select product to view inventory details");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "S":
                        Console.Write("Enter product ID to view inventory details: ");
                        var input = Console.ReadLine();
                        if (int.TryParse(input, out var productId))
                        {
                            var inv = inventoryService.GetInventoryByProductId(productId);
                            if (inv != null)
                            {
                                var product = productService.GetProductById(productId);
                                Console.WriteLine($"\n--- Inventory Details for Product ID: {productId} ---");
                                Console.WriteLine($"Product Name:    {product?.Name ?? "N/A"}");
                                Console.WriteLine($"Product SKU:     {product?.SKU ?? "N/A"}");
                                Console.WriteLine($"Quantity On Hand: {inv.QuantityOnHand}");
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Inventory record not found for this product.");
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError viewing inventory: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static void UpdateInventory(IInventoryService service)
    {
        Console.Write("Enter product ID: ");
        if (!int.TryParse(Console.ReadLine(), out var productId))
        {
            Console.WriteLine("Invalid product ID.");
            return;
        }

        Console.Write("Enter new quantity: ");
        if (!int.TryParse(Console.ReadLine(), out var quantity))
        {
            Console.WriteLine("Invalid quantity.");
            return;
        }

        service.UpdateInventory(productId, quantity);
        Console.WriteLine("Inventory updated.");
    }

    private static void AdjustInventory(IInventoryService service)
    {
        Console.Write("Enter product ID: ");
        if (!int.TryParse(Console.ReadLine(), out var productId))
        {
            Console.WriteLine("Invalid product ID.");
            return;
        }

        Console.Write("Enter quantity change (positive to add, negative to subtract): ");
        if (!int.TryParse(Console.ReadLine(), out var quantityChange))
        {
            Console.WriteLine("Invalid quantity change.");
            return;
        }

        service.AdjustInventory(productId, quantityChange);
        Console.WriteLine("Inventory adjusted.");
    }

    private static void GetStockQuantity(IInventoryService service)
    {
        Console.Write("Enter product ID: ");
        if (!int.TryParse(Console.ReadLine(), out var productId))
        {
            Console.WriteLine("Invalid product ID.");
            return;
        }

        var quantity = service.GetStockQuantity(productId);
        Console.WriteLine($"Stock quantity for product {productId}: {quantity}");
    }

    private static void GetLowStockProducts(IInventoryService service)
    {
        try
        {
            Console.Write("Enter stock threshold (products below this will be shown): ");
            if (!int.TryParse(Console.ReadLine(), out var threshold))
            {
                Console.WriteLine("Invalid threshold. Please enter a number.");
                return;
            }

            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = service.GetLowStockProductsPaged(threshold, currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve low stock products. ");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine($"\nNo products found with stock below {threshold}.");
                    return;
                }

                var lowStockItems = pagedResult.Items?.ToList() ?? new List<Inventory>();
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + lowStockItems.Count, pagedResult.TotalCount);

                Console.WriteLine($"\n--- Products with Stock Below {threshold} ---");
                Console.WriteLine($"Products (Page {currentPage} of {pagedResult.TotalPages})");
                Console.WriteLine($"{"Product ID",-12} {"Product Name",-50} {"Stock",-10}");
                Console.WriteLine(new string('-', 75));

                var productService = ServiceFactory.CreateProductService();
                foreach (var item in lowStockItems)
                {
                    var product = productService.GetProductById(item.ProductId);
                    var productName = product?.Name ?? "N/A";
                    var name = productName.Length > 48 ? productName.Substring(0, 45) + "..." : productName;
                    
                    Console.WriteLine($"{item.ProductId,-12} {name,-50} {item.QuantityOnHand,-10}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} products with low stock");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  S - Select product to view details");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "S":
                        Console.Write("Enter product ID to view details: ");
                        var input = Console.ReadLine();
                        if (int.TryParse(input, out var productId))
                        {
                            var product = productService.GetProductById(productId);
                            var inventory = service.GetInventoryByProductId(productId);
                            if (product != null)
                            {
                                Console.WriteLine($"\nProduct ID:   {product.ProductId}");
                                Console.WriteLine($"Name:         {product.Name}");
                                Console.WriteLine($"SKU:          {product.SKU}");
                                Console.WriteLine($"Price:        ${product.Price}");
                                Console.WriteLine($"Stock:        {inventory?.QuantityOnHand ?? 0}");
                                Console.WriteLine($"Description:  {product.Description}");
                                Console.WriteLine("\nPress any key to continue...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Product ID not found. Please try again.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, S, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError getting low stock products: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
   
    private static async Task BrowseProducts()
    {
        await CosmosLogger.LogAsync("browse_products", "guest_customer");
        
        var productService = ServiceFactory.CreateProductService();
        var categoryService = ServiceFactory.CreateCategoryService();
        var inventoryService = ServiceFactory.CreateInventoryService();
        bool back = false;

        while (!back)
        {
            Console.WriteLine("\n=== Browse Products ===");
            Console.WriteLine("1. View all active products");
            Console.WriteLine("2. Browse by category");
            Console.WriteLine("3. Search products");
            Console.WriteLine("4. View product by ID");
            Console.WriteLine("5. Back to customer portal");
            Console.Write("Select an option (1-5): ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    ListActiveProductsForCustomer(productService, inventoryService);
                    break;
                case "2":
                    BrowseByCategory(categoryService, productService, inventoryService);
                    break;
                case "3":
                    SearchProductsForCustomer(productService, inventoryService);
                    break;
                case "4":
                    ViewProductForCustomer(productService, inventoryService);
                    break;
                case "5":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void BrowseByCategory(ICategoryService categoryService, IProductService productService, IInventoryService inventoryService)
    {
        Console.WriteLine("\n=== Browse by Category ===");
        
        var categories = categoryService.GetActiveCategories().ToList();
        
        if (categories.Count == 0)
        {
            Console.WriteLine("No active categories available.");
            return;
        }

        Console.WriteLine("\n--- Available Categories ---");
        foreach (var category in categories)
        {
            Console.WriteLine($"{category.CategoryId}. {category.Name}");
        }

        Console.Write("\nEnter category ID to view products (or 0 to go back): ");
        if (!int.TryParse(Console.ReadLine(), out var categoryId) || categoryId == 0)
        {
            return;
        }

        var selectedCategory = categoryService.GetCategoryById(categoryId);
        if (selectedCategory == null)
        {
            Console.WriteLine("Category not found.");
            return;
        }

        if (selectedCategory.IsActive != "True")
        {
            Console.WriteLine("This category is not active.");
            return;
        }

        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = productService.GetProductsByCategoryPaged(categoryId, currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve products.");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine($"\nNo active products found in category '{selectedCategory.Name}'.");
                    return;
                }

                var products = (pagedResult.Items?.Take(pageSize).ToList() ?? new List<Product>()).Take(pageSize).ToList();
                var startIndex = (currentPage - 1) * pageSize;
                var actualCount = products.Count;
                var endIndex = startIndex + actualCount;

                Console.WriteLine($"\n--- Products in Category: {selectedCategory.Name} (Page {currentPage} of {pagedResult.TotalPages}) ---");
                Console.WriteLine($"{"ID",-8} {"Name",-50} {"Price",-12} {"Stock",-10}");
                Console.WriteLine(new string('-', 85));
                
                foreach (var p in products)
                {
                    var stock = inventoryService.GetStockQuantity(p.ProductId);
                    var price = p.Price?.ToString("F2") ?? "N/A";
                    var name = p.Name?.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name ?? "N/A";
                    
                    Console.WriteLine($"{p.ProductId,-8} {name,-50} ${price,-11} {stock,-10}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} products in this category ({actualCount} products on this page)");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  A - Add product to cart");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "A":
                        Console.Write("Enter product ID to add to cart (or 0 to cancel): ");
                        if (int.TryParse(Console.ReadLine(), out var productId) && productId > 0)
                        {
                            AddToCart(productId, productService, inventoryService).GetAwaiter().GetResult();
                            Console.WriteLine("\nPress any key to continue...");
                            Console.ReadKey();
                        }
                        else if (productId == 0)
                        {
                            
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, A, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError browsing products by category: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static void ListActiveProductsForCustomer(IProductService productService, IInventoryService inventoryService)
    {
        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = productService.GetActiveProductsPaged(currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve active products.");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine("\nNo active products available.");
                    return;
                }

                var products = (pagedResult.Items?.Take(pageSize).ToList() ?? new List<Product>()).Take(pageSize).ToList();
                var startIndex = (currentPage - 1) * pageSize;
                var actualCount = products.Count;
                var endIndex = startIndex + actualCount;

                Console.WriteLine($"\n--- Active Products (Page {currentPage} of {pagedResult.TotalPages}) ---");
                Console.WriteLine($"{"ID",-8} {"Name",-50} {"Price",-12} {"Stock",-10}");
                Console.WriteLine(new string('-', 85));
                
                foreach (var p in products)
                {
                    var stock = inventoryService.GetStockQuantity(p.ProductId);
                    var price = p.Price?.ToString("F2") ?? "N/A";
                    var name = p.Name?.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name ?? "N/A";
                    
                    Console.WriteLine($"{p.ProductId,-8} {name,-50} ${price,-11} {stock,-10}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} active products ({actualCount} products on this page)");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  A - Add product to cart");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "A":
                        Console.Write("Enter product ID to add to cart (or 0 to cancel): ");
                        if (int.TryParse(Console.ReadLine(), out var productId) && productId > 0)
                        {
                            AddToCart(productId, productService, inventoryService).GetAwaiter().GetResult();
                            Console.WriteLine("\nPress any key to continue...");
                            Console.ReadKey();
                        }
                        else if (productId == 0)
                        {
                            
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, A, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError listing active products: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static void SearchProductsForCustomer(IProductService productService, IInventoryService inventoryService)
    {
        Console.Write("Enter search term: ");
        var searchTerm = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            Console.WriteLine("Search term cannot be empty.");
            return;
        }

        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = productService.SearchProductsPaged(searchTerm, currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve search results.");
                    return;
                }
       
                var activeProducts = pagedResult.Items?.Where(p => p.IsActive == "True").ToList() ?? new List<Product>();
                
                if (pagedResult.TotalCount == 0 || activeProducts.Count == 0)
                {
                    Console.WriteLine($"\nNo active products found matching '{searchTerm}'.");
                    return;
                }

                var startIndex = (currentPage - 1) * pageSize;
                var actualCount = activeProducts.Count;
                var endIndex = startIndex + actualCount;

                Console.WriteLine($"\n--- Search Results for '{searchTerm}' (Page {currentPage} of {pagedResult.TotalPages}) ---");
                Console.WriteLine($"{"ID",-8} {"Name",-50} {"Price",-12} {"Stock",-10}");
                Console.WriteLine(new string('-', 85));
                
                foreach (var p in activeProducts)
                {
                    var stock = inventoryService.GetStockQuantity(p.ProductId);
                    var price = p.Price?.ToString("F2") ?? "N/A";
                    var name = p.Name?.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name ?? "N/A";
                    
                    Console.WriteLine($"{p.ProductId,-8} {name,-50} ${price,-11} {stock,-10}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} search results ({actualCount} active products on this page)");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  A - Add product to cart");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "A":
                        Console.Write("Enter product ID to add to cart (or 0 to cancel): ");
                        if (int.TryParse(Console.ReadLine(), out var productId) && productId > 0)
                        {
                            AddToCart(productId, productService, inventoryService).GetAwaiter().GetResult();
                            Console.WriteLine("\nPress any key to continue...");
                            Console.ReadKey();
                        }
                        else if (productId == 0)
                        {
                            
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, A, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError searching products: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static void ViewProductForCustomer(IProductService productService, IInventoryService inventoryService)
    {
        Console.Write("Enter product ID: ");
        if (!int.TryParse(Console.ReadLine(), out var productId))
        {
            Console.WriteLine("Invalid product ID.");
            return;
        }

        var product = productService.GetProductById(productId);
        if (product == null)
        {
            Console.WriteLine("Product not found.");
            return;
        }

        if (product.IsActive != "True")
        {
            Console.WriteLine("This product is not active.");
            return;
        }

        var stock = inventoryService.GetStockQuantity(productId);
        var price = product.Price?.ToString("F2") ?? "N/A";

        Console.WriteLine($"\n--- Product Details ---");
        Console.WriteLine($"ID:          {product.ProductId}");
        Console.WriteLine($"Name:        {product.Name}");
        Console.WriteLine($"SKU:         {product.SKU}");
        Console.WriteLine($"Price:       ${price}");
        Console.WriteLine($"Description: {product.Description ?? "N/A"}");
        Console.WriteLine($"Stock:       {stock}");

        Console.Write("\nAdd to cart? (y/n): ");
        var response = Console.ReadLine();
        if (response?.ToLower() == "y")
        {
            AddToCart(productId, productService, inventoryService).GetAwaiter().GetResult();
        }
    }

    private static async Task AddToCart(int productId, IProductService productService, IInventoryService inventoryService)
    {
        var product = productService.GetProductById(productId);
        if (product == null)
        {
            Console.WriteLine("Product not found.");
            return;
        }

        if (product.IsActive != "True")
        {
            Console.WriteLine("This product is not active.");
            return;
        }

        var stock = inventoryService.GetStockQuantity(productId);
        var price = product.Price ?? 0;

        Console.WriteLine($"\nProduct: {product.Name}");
        Console.WriteLine($"Price: ${price:F2}");
        Console.WriteLine($"Available Stock: {stock}");

        Console.Write("Enter quantity: ");
        if (!int.TryParse(Console.ReadLine(), out var quantity) || quantity <= 0)
        {
            Console.WriteLine("Invalid quantity.");
            return;
        }

        if (quantity > stock)
        {
            Console.WriteLine($"Insufficient stock. Available: {stock}");
            return;
        }

        var existingItem = _shoppingCart.FirstOrDefault(c => c.ProductId == productId);
        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + quantity;
            if (newQuantity > stock)
            {
                Console.WriteLine($"Cannot add {quantity} more. Total would exceed available stock ({stock}).");
                Console.WriteLine($"Current in cart: {existingItem.Quantity}, Available: {stock}");
                return;
            }
            existingItem.Quantity = newQuantity;
            Console.WriteLine($"Updated quantity. Total in cart: {existingItem.Quantity} x {product.Name}");
            await CosmosLogger.LogAsync("add_to_cart", "guest_customer");
        }
        else
        {
            _shoppingCart.Add(new CartItem
            {
                ProductId = productId,
                ProductName = product.Name,
                UnitPrice = price,
                Quantity = quantity
            });
            Console.WriteLine($"Added {quantity} x {product.Name} to cart");
            await CosmosLogger.LogAsync("add_to_cart", "guest_customer");
        }
    }

    private static void ViewShoppingCart()
    {
        bool back = false;
        while (!back)
        {
            if (_shoppingCart.Count == 0)
            {
                Console.WriteLine("\nYour shopping cart is empty.");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("1. Add products to cart");
                Console.WriteLine("2. Back to customer portal");
                Console.Write("Select an option (1-2): ");

                var emptyChoice = Console.ReadLine();
                switch (emptyChoice)
                {
                    case "1":
                        AddProductToCartFromCartView();
                        break;
                    case "2":
                        back = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
                continue;
            }

            Console.WriteLine("\n=== Shopping Cart ===");
            decimal total = 0;
            
            for (int i = 0; i < _shoppingCart.Count; i++)
            {
                var item = _shoppingCart[i];
                total += item.LineTotal;
                Console.WriteLine($"{i + 1}. {item.ProductName} (ID: {item.ProductId})");
                Console.WriteLine($"   Quantity: {item.Quantity} x ${item.UnitPrice:F2} = ${item.LineTotal:F2}");
            }

            Console.WriteLine($"\n--- Cart Total: ${total:F2} ---");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("1. Add new product to cart");
            Console.WriteLine("2. Update item quantity");
            Console.WriteLine("3. Remove item");
            Console.WriteLine("4. Clear cart");
            Console.WriteLine("5. Back to customer portal");
            Console.Write("Select an option (1-5): ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    AddProductToCartFromCartView();
                    break;
                case "2":
                    UpdateCartItem();
                    break;
                case "3":
                    RemoveCartItem();
                    break;
                case "4":
                    _shoppingCart.Clear();
                    Console.WriteLine("Cart cleared.");
                    break;
                case "5":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void AddProductToCartFromCartView()
    {
        var productService = ServiceFactory.CreateProductService();
        var inventoryService = ServiceFactory.CreateInventoryService();
        bool back = false;

        while (!back)
        {
            Console.WriteLine("\n=== Add Product to Cart ===");
            Console.WriteLine("1. View all active products");
            Console.WriteLine("2. Browse by category");
            Console.WriteLine("3. Search products");
            Console.WriteLine("4. Enter product ID directly");
            Console.WriteLine("5. Back to cart");
            Console.Write("Select an option (1-5): ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    ListActiveProductsForCart(productService, inventoryService);
                    break;
                case "2":
                    BrowseByCategoryForCart(productService, inventoryService);
                    break;
                case "3":
                    SearchProductsForCart(productService, inventoryService);
                    break;
                case "4":
                    AddProductByIdDirectly(productService, inventoryService);
                    break;
                case "5":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void ListActiveProductsForCart(IProductService productService, IInventoryService inventoryService)
    {
        try
        {
            const int pageSize = 50;
            int currentPage = 1;
            bool viewing = true;

            while (viewing)
            {
                var pagedResult = productService.GetActiveProductsPaged(currentPage, pageSize);
                
                if (pagedResult == null)
                {
                    Console.WriteLine("\nError: Failed to retrieve active products.");
                    return;
                }
                
                if (pagedResult.TotalCount == 0)
                {
                    Console.WriteLine("\nNo active products available.");
                    return;
                }

                var products = pagedResult.Items?.ToList() ?? new List<Product>();
                var startIndex = (currentPage - 1) * pageSize;
                var actualCount = products.Count;
                var endIndex = startIndex + actualCount;

                Console.WriteLine($"\n--- Active Products (Page {currentPage} of {pagedResult.TotalPages}) ---");
                Console.WriteLine($"{"ID",-8} {"Name",-50} {"Price",-12} {"Stock",-10}");
                Console.WriteLine(new string('-', 85));
                
                foreach (var p in products)
                {
                    var stock = inventoryService.GetStockQuantity(p.ProductId);
                    var price = p.Price?.ToString("F2") ?? "N/A";
                    var name = p.Name?.Length > 48 ? p.Name.Substring(0, 45) + "..." : p.Name ?? "N/A";
                    
                    Console.WriteLine($"{p.ProductId,-8} {name,-50} ${price,-11} {stock,-10}");
                }

                Console.WriteLine($"\nShowing {startIndex + 1}-{endIndex} of {pagedResult.TotalCount} active products ({actualCount} products on this page)");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  N - Next page");
                Console.WriteLine("  P - Previous page");
                Console.WriteLine("  A - Add product to cart");
                Console.WriteLine("  Q - Quit (go back)");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < pagedResult.TotalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;

                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;

                    case "A":
                        Console.Write("Enter product ID to add to cart (or 0 to cancel): ");
                        if (int.TryParse(Console.ReadLine(), out var productId) && productId > 0)
                        {
                            AddToCart(productId, productService, inventoryService).GetAwaiter().GetResult();
                            Console.WriteLine("\nPress any key to continue...");
                            Console.ReadKey();
                        }
                        else if (productId == 0)
                        {
                            
                        }
                        else
                        {
                            Console.WriteLine("Invalid product ID. Please enter a number.");
                        }
                        break;

                    case "Q":
                        viewing = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, A, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError listing active products: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static void BrowseByCategoryForCart(IProductService productService, IInventoryService inventoryService)
    {
        var categoryService = ServiceFactory.CreateCategoryService();
        BrowseByCategory(categoryService, productService, inventoryService);
    }

    private static void SearchProductsForCart(IProductService productService, IInventoryService inventoryService)
    {
        SearchProductsForCustomer(productService, inventoryService);
    }

    private static void AddProductByIdDirectly(IProductService productService, IInventoryService inventoryService)
    {
        Console.Write("Enter product ID: ");
        if (!int.TryParse(Console.ReadLine(), out var productId))
        {
            Console.WriteLine("Invalid product ID.");
            return;
        }

        var product = productService.GetProductById(productId);
        if (product == null)
        {
            Console.WriteLine("Product not found.");
            return;
        }

        if (product.IsActive != "True")
        {
            Console.WriteLine("This product is not active.");
            return;
        }

        AddToCart(productId, productService, inventoryService).GetAwaiter().GetResult();
    }

    private static void UpdateCartItem()
    {
        if (_shoppingCart.Count == 0)
        {
            Console.WriteLine("Cart is empty.");
            return;
        }

        Console.Write("Enter item number to update: ");
        if (!int.TryParse(Console.ReadLine(), out var itemNum) || itemNum < 1 || itemNum > _shoppingCart.Count)
        {
            Console.WriteLine("Invalid item number.");
            return;
        }

        var item = _shoppingCart[itemNum - 1];
        var inventoryService = ServiceFactory.CreateInventoryService();
        var stock = inventoryService.GetStockQuantity(item.ProductId);

        Console.WriteLine($"Current quantity: {item.Quantity}");
        Console.WriteLine($"Available stock: {stock}");
        Console.Write("Enter new quantity (or 0 to remove): ");
        
        if (!int.TryParse(Console.ReadLine(), out var newQuantity))
        {
            Console.WriteLine("Invalid quantity.");
            return;
        }

        if (newQuantity == 0)
        {
            _shoppingCart.RemoveAt(itemNum - 1);
            Console.WriteLine("Item removed from cart.");
        }
        else if (newQuantity < 0)
        {
            Console.WriteLine("Quantity cannot be negative.");
        }
        else if (newQuantity > stock)
        {
            Console.WriteLine($"Insufficient stock. Available: {stock}");
        }
        else
        {
            item.Quantity = newQuantity;
            Console.WriteLine($"Updated quantity to {newQuantity}.");
        }
    }

    private static void RemoveCartItem()
    {
        if (_shoppingCart.Count == 0)
        {
            Console.WriteLine("Cart is empty.");
            return;
        }

        Console.Write("Enter item number to remove: ");
        if (!int.TryParse(Console.ReadLine(), out var itemNum) || itemNum < 1 || itemNum > _shoppingCart.Count)
        {
            Console.WriteLine("Invalid item number.");
            return;
        }

        var item = _shoppingCart[itemNum - 1];
        _shoppingCart.RemoveAt(itemNum - 1);
        Console.WriteLine($"Removed {item.ProductName} from cart.");
    }

    private static async Task Checkout()
    {
        if (_shoppingCart.Count == 0)
        {
            Console.WriteLine("\nYour shopping cart is empty. Please add items before checkout.");
            return;
        }

        Console.WriteLine("\n=== Checkout ===");
        
        Console.WriteLine("\n--- Order Summary ---");
        decimal total = 0;
        foreach (var item in _shoppingCart)
        {
            total += item.LineTotal;
            Console.WriteLine($"{item.ProductName}: {item.Quantity} x ${item.UnitPrice:F2} = ${item.LineTotal:F2}");
        }
        Console.WriteLine($"\nTotal: ${total:F2}");

        Console.Write("\nProceed with checkout? (y/n): ");
        var confirm = Console.ReadLine();
        if (confirm?.ToLower() != "y")
        {
            Console.WriteLine("Checkout cancelled.");
            return;
        }

        Console.WriteLine("\n--- Customer Information ---");
        Console.Write("Enter full name: ");
        var fullName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            Console.WriteLine("Name is required.");
            return;
        }

        Console.Write("Enter email: ");
        var email = Console.ReadLine();

        Console.Write("Enter phone: ");
        var phone = Console.ReadLine();

        Console.Write("Enter address line: ");
        var addressLine = Console.ReadLine();

        Console.Write("Enter city: ");
        var city = Console.ReadLine();

        var adminId = GetDefaultAdminId();

        try
        {
            var customerService = ServiceFactory.CreateCustomerService();
            var orderService = ServiceFactory.CreateOrderService();
            var inventoryService = ServiceFactory.CreateInventoryService();

            var customer = new Customer
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                AddressLine = addressLine,
                City = city
            };

            var createdCustomer = customerService.CreateCustomer(customer);
            Console.WriteLine($"\nCustomer created with ID {createdCustomer.CustomerId}");

            var order = orderService.CreateOrder(createdCustomer.CustomerId, adminId, "Pending");
            Console.WriteLine($"Order created with ID {order.OrderId}");

            foreach (var cartItem in _shoppingCart)
            {
                var stock = inventoryService.GetStockQuantity(cartItem.ProductId);
                if (cartItem.Quantity > stock)
                {
                    Console.WriteLine($"Warning: {cartItem.ProductName} - Requested {cartItem.Quantity}, but only {stock} available.");
                    Console.Write("Continue with available stock? (y/n): ");
                    var continueResponse = Console.ReadLine();
                    if (continueResponse?.ToLower() != "y")
                    {
                        Console.WriteLine("Order cancelled. Please update your cart.");
                        return;
                    }
                    cartItem.Quantity = stock;
                }

                orderService.AddOrderItem(order.OrderId, cartItem.ProductId, cartItem.Quantity, cartItem.UnitPrice);
            }

            var finalOrder = orderService.GetOrderById(order.OrderId);
            Console.WriteLine($"\n=== Order Placed Successfully ===");
            Console.WriteLine($"Order ID: {finalOrder?.OrderId}");
            Console.WriteLine($"Total: ${finalOrder?.TotalAmount:F2}");
            Console.WriteLine($"Status: {finalOrder?.Status}");
            Console.WriteLine("\nOrder items:");
            foreach (var item in orderService.GetOrderItems(order.OrderId))
            {
                var product = ServiceFactory.CreateProductService().GetProductById(item.ProductId);
                Console.WriteLine($"  - {product?.Name ?? $"Product {item.ProductId}"}: {item.Quantity} x ${item.UnitPriceAtOrder:F2} = ${item.LineTotalAmount:F2}");
            }

            _shoppingCart.Clear();
            Console.WriteLine("\nThank you for your order!");
            
            await CosmosLogger.LogAsync("guest_checkout", createdCustomer.CustomerId.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError during checkout: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Details: {ex.InnerException.Message}");
            }
        }
    }
  
    private static void TestDatabaseConnection()
    {
        Console.WriteLine("\n=== Database Connection Test ===");
        
        try
        {
            Console.WriteLine("\n1. Testing direct SQL connection...");
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            Console.WriteLine("SQL connection successful!");
            
            Console.WriteLine("\n2. Testing stored procedure access...");
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Customer", conn);
            var customerCount = (int)cmd.ExecuteScalar();
            Console.WriteLine($"Stored procedure access successful!");
            Console.WriteLine($"Found {customerCount} customer(s) in database");
           
            Console.WriteLine("\n3. Testing EF Core connection...");
            try
            {
                using var efContext = new ShopRAR.BLL.EF.AppDbContext(ConnectionString);
                var efCustomerCount = efContext.Customers.Count();
                Console.WriteLine($"EF Core connection successful!");
                Console.WriteLine($"EF Core can access database (found {efCustomerCount} customer(s))");
            }
            catch (Exception efEx)
            {
                Console.WriteLine($"EF Core connection test failed: {efEx.Message}");
                if (efEx.InnerException != null)
                {
                    Console.WriteLine($"   Inner exception: {efEx.InnerException.Message}");
                }
            }
            
            Console.WriteLine("\n4. Testing stored procedures...");
            using var spCmd = new SqlCommand("SELECT COUNT(*) FROM sys.procedures WHERE name = 'usp_GetAllCustomers'", conn);
            var spExists = (int)spCmd.ExecuteScalar();
            if (spExists > 0)
            {
                Console.WriteLine("Stored procedure exists");
            }
            else
            {
                Console.WriteLine("Warning: Stored procedure not found");
            }
            
            Console.WriteLine("\n5. Database information:");
            using var dbCmd = new SqlCommand("SELECT DB_NAME(), @@VERSION", conn);
            using var reader = dbCmd.ExecuteReader();
            if (reader.Read())
            {
                Console.WriteLine($"   Database: {reader[0]}");
                Console.WriteLine($"   Server: {conn.DataSource}");
            }
            
            Console.WriteLine("\n=== All connection tests passed! ===");
            Console.WriteLine("Your application is successfully connected to Azure SQL Database.");
        }
        catch (SqlException sqlEx)
        {
            Console.WriteLine($"\nSQL Connection Error:");
            Console.WriteLine($"   Error Number: {sqlEx.Number}");
            Console.WriteLine($"   Error Message: {sqlEx.Message}");
            Console.WriteLine($"   Server: {sqlEx.Server}");
            Console.WriteLine("\nCommon issues:");
            Console.WriteLine("  - Check if your Azure SQL firewall allows your IP address");
            Console.WriteLine("  - Verify the connection string (server, database, credentials)");
            Console.WriteLine("  - Ensure the database exists and is accessible");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nConnection Error:");
            Console.WriteLine($"   Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"   Error Type: {ex.GetType().Name}");
        }
        
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
   
    private static void ReportsMenu()
    {
        var service = ServiceFactory.CreateReportingService();
        bool back = false;

        while (!back)
        {
            Console.WriteLine("\n=== Reports & Analytics ===");
            Console.WriteLine("1. Top Selling Products (by month)");
            Console.WriteLine("2. Customer Lifetime Spend");
            Console.WriteLine("3. Back to main menu");
            Console.Write("Select an option (1-3): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ViewTopSellingProducts(service);
                    break;
                case "2":
                    ViewCustomerLifetimeSpend(service);
                    break;
                case "3":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static void ViewTopSellingProducts(IReportingService service)
    {
        try
        {
            Console.Write("\nEnter year: ");
            if (!int.TryParse(Console.ReadLine(), out var year) || year < 2000 || year > 2100)
            {
                Console.WriteLine("Invalid year. Please enter a valid year (2000-2100).");
                return;
            }

            Console.Write("Enter month (1-12): ");
            if (!int.TryParse(Console.ReadLine(), out var month) || month < 1 || month > 12)
            {
                Console.WriteLine("Invalid month. Please enter a value between 1 and 12.");
                return;
            }

            Console.Write("Enter number of top products to show (default: 10): ");
            var topNInput = Console.ReadLine();
            var topN = string.IsNullOrWhiteSpace(topNInput) ? 10 : int.Parse(topNInput);
            if (topN <= 0) topN = 10;

            var products = service.GetTopSellingProducts(year, month, topN).ToList();

            if (products.Count == 0)
            {
                Console.WriteLine($"\nNo sales data found for {month}/{year}.");
                return;
            }

            Console.WriteLine($"\n=== Top {products.Count} Selling Products - {month}/{year} ===");
            Console.WriteLine($"{"Rank",-6} {"Product ID",-12} {"Product Name",-50} {"Quantity",-12} {"Revenue",-15}");
            Console.WriteLine(new string('-', 100));

            int rank = 1;
            foreach (var product in products)
            {
                var name = product.ProductName.Length > 48 ? product.ProductName.Substring(0, 45) + "..." : product.ProductName;
                Console.WriteLine($"{rank,-6} {product.ProductId,-12} {name,-50} {product.TotalQty,-12} ${product.TotalRevenue:F2}");
                rank++;
            }

            var totalRevenue = products.Sum(p => p.TotalRevenue);
            var totalQty = products.Sum(p => p.TotalQty);
            Console.WriteLine(new string('-', 100));
            Console.WriteLine($"{"TOTAL",-6} {"",-12} {"",-50} {totalQty,-12} ${totalRevenue:F2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError generating report: {ex.Message}");
        }
    }

    private static void ViewCustomerLifetimeSpend(IReportingService service)
    {
        try
        {
            const int pageSize = 20;
            int currentPage = 1;
            int totalPages = 1;
            PagedResult<CustomerLifetimeSpend>? pagedResult = null;

            while (true)
            {
                pagedResult = service.GetCustomerLifetimeSpendPaged(currentPage, pageSize);
                totalPages = pagedResult.TotalPages;

                if (pagedResult.Items == null || !pagedResult.Items.Any())
                {
                    Console.WriteLine("\nNo customer data found.");
                    return;
                }

                var customers = pagedResult.Items.ToList();
                var startRank = (currentPage - 1) * pageSize + 1;

                Console.WriteLine($"\n=== Customer Lifetime Spend Report (Page {currentPage} of {totalPages}) ===");
                Console.WriteLine($"Total Customers: {pagedResult.TotalCount}");
                Console.WriteLine($"{"Rank",-6} {"Customer ID",-14} {"Customer Name",-40} {"Orders",-10} {"Lifetime Spend",-18} {"Last Order",-15}");
                Console.WriteLine(new string('-', 110));

                int rank = startRank;
                foreach (var customer in customers)
                {
                    var name = customer.FullName.Length > 38 ? customer.FullName.Substring(0, 35) + "..." : customer.FullName;
                    var lastOrder = customer.LastOrderDate?.ToString("MM/dd/yyyy") ?? "N/A";
                    Console.WriteLine($"{rank,-6} {customer.CustomerId,-14} {name,-40} {customer.OrderCount,-10} ${customer.LifetimeSpend,-17:F2} {lastOrder,-15}");
                    rank++;
                }

                Console.WriteLine(new string('-', 110));
                Console.WriteLine($"Showing {startRank}-{startRank + customers.Count - 1} of {pagedResult.TotalCount} customers");

                if (totalPages <= 1)
                {
                    break;
                }

                Console.WriteLine("\nNavigation:");
                if (currentPage > 1)
                {
                    Console.WriteLine("  P - Previous page");
                }
                if (currentPage < totalPages)
                {
                    Console.WriteLine("  N - Next page");
                }
                Console.WriteLine("  Q - Quit");
                Console.Write("\nEnter option: ");

                var option = Console.ReadLine()?.Trim().ToUpper();

                switch (option)
                {
                    case "N":
                        if (currentPage < totalPages)
                        {
                            currentPage++;
                        }
                        else
                        {
                            Console.WriteLine("Already on the last page.");
                        }
                        break;
                    case "P":
                        if (currentPage > 1)
                        {
                            currentPage--;
                        }
                        else
                        {
                            Console.WriteLine("Already on the first page.");
                        }
                        break;
                    case "Q":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please enter N, P, or Q.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError generating report: {ex.Message}");
        }
    }
 
}

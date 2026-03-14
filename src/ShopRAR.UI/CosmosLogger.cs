using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ShopRAR.UI;

public static class CosmosLogger
{
    private static CosmosClient? _client;
    private static Container? _container;
    private static bool _initialized = false;

    public static void Initialize(string connectionString)
    {
        try
        {
            Console.WriteLine("Initializing Cosmos DB logger...");
            _client = new CosmosClient(connectionString);

            Console.WriteLine("Creating/accessing database 'ShopRARLogs'...");
            var database = _client.CreateDatabaseIfNotExistsAsync("ShopRARLogs").GetAwaiter().GetResult();

            Console.WriteLine("Creating/accessing container 'ActivityLogs'...");
            _container = database.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties("ActivityLogs", "/customerId")
            ).GetAwaiter().GetResult().Container;
            
            _initialized = true;
            Console.WriteLine("Cosmos DB logger initialized successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to initialize Cosmos DB logger: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
            _initialized = false;
        }
    }

    public static async Task LogAsync(string action, string customerId)
    {
        if (!_initialized)
        {
            Console.WriteLine($"Cosmos logger not initialized. Skipping log for action: {action}");
            return;
        }
        
        if (_container == null)
        {
            Console.WriteLine($"Cosmos container is null. Skipping log for action: {action}");
            return;
        }

        try
        {
            var log = new
            {
                id = Guid.NewGuid().ToString(),
                timestamp = DateTime.UtcNow,
                action = action,
                customerId = customerId
            };

            Console.WriteLine($"Logging to Cosmos DB: action={action}, customerId={customerId}");
            await _container.CreateItemAsync(log, new PartitionKey(customerId));
            Console.WriteLine($"Successfully logged to Cosmos DB: {action}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to log to Cosmos DB: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"   Action: {action}, CustomerId: {customerId}");
        }
    }
}
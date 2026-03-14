using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ShopRAR.Domain;

namespace ShopRAR.BLL.SP;

public class SpProductService : IProductService
{
    private readonly string _connectionString;

    public SpProductService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IEnumerable<Product> GetAllProducts()
    {
        var result = new List<Product>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetAllProducts", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapProduct(reader));
        }

        return result;
    }

    public PagedResult<Product> GetAllProductsPaged(int pageNumber, int pageSize)
    {
        var result = new PagedResult<Product>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<Product>()
        };

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetAllProductsPaged", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        
        var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(totalCountParam);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        var items = new List<Product>();
        while (reader.Read())
        {
            items.Add(MapProduct(reader));
        }
        
        reader.Close();
        
        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }

    public Product? GetProductById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetProductById", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", id);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapProduct(reader);
        }

        return null;
    }

    public Product CreateProduct(Product product)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_CreateProduct", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Name", product.Name);
        cmd.Parameters.AddWithValue("@SKU", (object?)product.SKU ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Price", (object?)product.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", (object?)product.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", (object?)product.IsActive ?? DBNull.Value);

        var idParam = new SqlParameter("@NewProductId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(idParam);

        var skuParam = new SqlParameter("@NewSKU", SqlDbType.NVarChar, 50)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(skuParam);

        conn.Open();
        cmd.ExecuteNonQuery();

        product.ProductId = (int)idParam.Value;
        
        if (skuParam.Value != DBNull.Value && skuParam.Value != null)
        {
            product.SKU = skuParam.Value.ToString();
        }
        return product;
    }

    public void UpdateProduct(Product product)
    {
       
        var existing = GetProductById(product.ProductId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Product with ID {product.ProductId} does not exist.");
        }

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_UpdateProduct", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
        cmd.Parameters.AddWithValue("@Name", product.Name);
        cmd.Parameters.AddWithValue("@SKU", (object?)existing.SKU ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Price", (object?)product.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", (object?)product.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", (object?)product.IsActive ?? DBNull.Value);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void DeleteProduct(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_DeleteProduct", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", id);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public PagedResult<Product> GetActiveProductsPaged(int pageNumber, int pageSize)
    {
        var result = new PagedResult<Product>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<Product>()
        };

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetActiveProductsPaged", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        
        var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(totalCountParam);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        var items = new List<Product>();
        while (reader.Read())
        {
            items.Add(MapProduct(reader));
        }
       
        reader.Close();
        
        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }

    public PagedResult<Product> GetProductsByCategoryPaged(int categoryId, int pageNumber, int pageSize)
    {
        var result = new PagedResult<Product>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<Product>()
        };

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetProductsByCategoryPaged", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CategoryId", categoryId);
        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        
        var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(totalCountParam);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        var items = new List<Product>();
        while (reader.Read())
        {
            items.Add(MapProduct(reader));
        }
        
        reader.Close();
        
        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }

    public PagedResult<Product> SearchProductsPaged(string searchTerm, int pageNumber, int pageSize)
    {
        var result = new PagedResult<Product>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<Product>()
        };

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_SearchProductsPaged", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);
        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        
        var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(totalCountParam);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        var items = new List<Product>();
        while (reader.Read())
        {
            items.Add(MapProduct(reader));
        }
       
        reader.Close();
        
        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }

    private static Product MapProduct(SqlDataReader reader)
    {
        return new Product
        {
            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            SKU = reader.IsDBNull(reader.GetOrdinal("SKU")) ? null : reader.GetString(reader.GetOrdinal("SKU")),
            Price = reader.IsDBNull(reader.GetOrdinal("Price")) ? null : reader.GetDecimal(reader.GetOrdinal("Price")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? null : reader.GetString(reader.GetOrdinal("IsActive"))
        };
    }

    private static Product MapProductFromView(SqlDataReader reader)
    {
        return new Product
        {
            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
            Name = reader.GetString(reader.GetOrdinal("ProductName")),
            SKU = reader.IsDBNull(reader.GetOrdinal("SKU")) ? null : reader.GetString(reader.GetOrdinal("SKU")),
            Price = reader.IsDBNull(reader.GetOrdinal("Price")) ? null : reader.GetDecimal(reader.GetOrdinal("Price")),
            Description = null, // Not in view
            IsActive = reader.IsDBNull(reader.GetOrdinal("ProductIsActive")) ? null : reader.GetString(reader.GetOrdinal("ProductIsActive"))
        };
    }
}


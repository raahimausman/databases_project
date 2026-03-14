using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ShopRAR.Domain;

namespace ShopRAR.BLL.SP;

public class SpCategoryService : ICategoryService
{
    private readonly string _connectionString;

    public SpCategoryService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IEnumerable<Category> GetAllCategories()
    {
        var result = new List<Category>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetAllCategories", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapCategory(reader));
        }

        return result;
    }

    public Category? GetCategoryById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetCategoryById", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CategoryId", id);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapCategory(reader);
        }

        return null;
    }

    public Category CreateCategory(Category category)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_CreateCategory", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Name", category.Name);
        cmd.Parameters.AddWithValue("@IsActive", (object?)category.IsActive ?? DBNull.Value);

        var idParam = new SqlParameter("@NewCategoryId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(idParam);

        conn.Open();
        cmd.ExecuteNonQuery();

        category.CategoryId = (int)idParam.Value;
        return category;
    }

    public void UpdateCategory(Category category)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_UpdateCategory", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CategoryId", category.CategoryId);
        cmd.Parameters.AddWithValue("@Name", category.Name);
        cmd.Parameters.AddWithValue("@IsActive", (object?)category.IsActive ?? DBNull.Value);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void DeleteCategory(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_DeleteCategory", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CategoryId", id);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<Category> GetActiveCategories()
    {
        var result = new List<Category>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetActiveCategories", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapCategory(reader));
        }

        return result;
    }

    private static Category MapCategory(SqlDataReader reader)
    {
        return new Category
        {
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? null : reader.GetString(reader.GetOrdinal("IsActive"))
        };
    }
}


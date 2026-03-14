using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using ShopRAR.Domain;

namespace ShopRAR.BLL.SP;

public class SpCustomerService : ICustomerService
{
    private readonly string _connectionString;

    public SpCustomerService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IEnumerable<Customer> GetAllCustomers()
    {
        var result = new List<Customer>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetAllCustomers", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapCustomer(reader));
        }

        return result;
    }

    public PagedResult<Customer> GetAllCustomersPaged(int pageNumber, int pageSize)
    {
        var result = new PagedResult<Customer>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<Customer>()
        };

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetAllCustomersPaged", conn)
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
        var items = new List<Customer>();
        while (reader.Read())
        {
            items.Add(MapCustomer(reader));
        }
        
        reader.Close();
        
        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }

    public Customer? GetCustomerById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetCustomerById", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CustomerId", id);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapCustomer(reader);
        }

        return null;
    }

    public PagedResult<Customer> SearchCustomersByNamePaged(string searchTerm, int pageNumber, int pageSize)
    {
        var result = new PagedResult<Customer>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<Customer>()
        };

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            result.TotalCount = 0;
            return result;
        }

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_SearchCustomersByNamePaged", conn)
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
        var items = new List<Customer>();
        while (reader.Read())
        {
            items.Add(MapCustomer(reader));
        }

        reader.Close();

        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }

    public Customer CreateCustomer(Customer customer)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_CreateCustomer", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@FullName", customer.FullName);
        cmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AddressLine", (object?)customer.AddressLine ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@City", (object?)customer.City ?? DBNull.Value);

        var idParam = new SqlParameter("@NewCustomerId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(idParam);

        conn.Open();
        cmd.ExecuteNonQuery();

        customer.CustomerId = (int)idParam.Value;
        return customer;
    }

    public void UpdateCustomer(Customer customer)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_UpdateCustomer", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
        cmd.Parameters.AddWithValue("@FullName", customer.FullName);
        cmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AddressLine", (object?)customer.AddressLine ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@City", (object?)customer.City ?? DBNull.Value);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void DeleteCustomer(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_DeleteCustomer", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CustomerId", id);

        conn.Open();
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number == 50000)
            {
                throw new InvalidOperationException(sqlEx.Message, sqlEx);
            }
            throw;
        }
    }

    private static Customer MapCustomer(SqlDataReader reader)
    {
        return new Customer
        {
            CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
            AddressLine = reader.IsDBNull(reader.GetOrdinal("AddressLine")) ? null : reader.GetString(reader.GetOrdinal("AddressLine")),
            City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City"))
        };
    }
}


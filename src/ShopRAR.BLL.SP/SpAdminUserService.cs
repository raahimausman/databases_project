using System.Data;
using Microsoft.Data.SqlClient;
using ShopRAR.Domain;

namespace ShopRAR.BLL.SP;

public class SpAdminUserService : IAdminUserService
{
    private readonly string _connectionString;

    public SpAdminUserService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public AdminUser? AuthenticateAdmin(string email, string passwordHash)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_AuthenticateAdmin", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapAdminUser(reader);
        }

        return null;
    }

    public AdminUser? GetAdminById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetAdminById", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@AdminId", id);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapAdminUser(reader);
        }

        return null;
    }

    public AdminUser CreateAdmin(string email, string passwordHash)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_CreateAdmin", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

        var idParam = new SqlParameter("@NewAdminId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(idParam);

        conn.Open();
        cmd.ExecuteNonQuery();

        return new AdminUser
        {
            AdminUserId = (int)idParam.Value,
            Email = email,
            PasswordHash = passwordHash
        };
    }

    public void UpdateAdmin(int adminId, string email, string? passwordHash = null)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_UpdateAdmin", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@AdminId", adminId);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@PasswordHash", (object?)passwordHash ?? DBNull.Value);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    private static AdminUser MapAdminUser(SqlDataReader reader)
    {
        return new AdminUser
        {
            AdminUserId = reader.GetInt32(reader.GetOrdinal("AdminUserId")),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
            PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? null : reader.GetString(reader.GetOrdinal("PasswordHash"))
        };
    }
}


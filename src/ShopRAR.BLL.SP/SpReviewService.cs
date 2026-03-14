using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ShopRAR.Domain;

namespace ShopRAR.BLL.SP;

public class SpReviewService : IReviewService
{
    private readonly string _connectionString;

    public SpReviewService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public PagedResult<Review> GetAllReviewsPaged(int pageNumber, int pageSize)
    {
        var result = new PagedResult<Review>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = new List<Review>()
        };

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetAllReviewsPaged", conn)
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
        var items = new List<Review>();
        while (reader.Read())
        {
            items.Add(MapReview(reader));
        }
        
        reader.Close();
        
        result.Items = items;
        result.TotalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

        return result;
    }

    public Review? GetReviewById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetReviewById", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ReviewId", id);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapReview(reader);
        }

        return null;
    }

    public Review CreateReview(Review review)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_CreateReview", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", review.ProductId);
        cmd.Parameters.AddWithValue("@CustomerId", review.CustomerId);
        cmd.Parameters.AddWithValue("@Rating", review.Rating);
        cmd.Parameters.AddWithValue("@Comments", (object?)review.Comments ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsApproved", (object?)review.IsApproved ?? DBNull.Value);

        var idParam = new SqlParameter("@NewReviewId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(idParam);

        conn.Open();
        cmd.ExecuteNonQuery();

        review.ReviewId = (int)idParam.Value;
        return review;
    }

    public void UpdateReview(Review review)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_UpdateReview", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ReviewId", review.ReviewId);
        cmd.Parameters.AddWithValue("@Rating", review.Rating);
        cmd.Parameters.AddWithValue("@Comments", (object?)review.Comments ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsApproved", (object?)review.IsApproved ?? DBNull.Value);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void DeleteReview(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_DeleteReview", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ReviewId", id);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<Review> GetReviewsByProduct(int productId)
    {
        var result = new List<Review>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetReviewsByProduct", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", productId);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapReview(reader));
        }

        return result;
    }

    public IEnumerable<Review> GetApprovedReviews(int productId)
    {
        var result = new List<Review>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_GetApprovedReviews", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ProductId", productId);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapReview(reader));
        }

        return result;
    }

    public void ApproveReview(int reviewId)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_ApproveReview", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ReviewId", reviewId);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void RejectReview(int reviewId)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_RejectReview", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ReviewId", reviewId);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    private static Review MapReview(SqlDataReader reader)
    {
        return new Review
        {
            ReviewId = reader.GetInt32(reader.GetOrdinal("ReviewId")),
            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
            CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
            Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
            Comments = reader.IsDBNull(reader.GetOrdinal("Comments")) ? null : reader.GetString(reader.GetOrdinal("Comments")),
            IsApproved = reader.IsDBNull(reader.GetOrdinal("IsApproved")) ? null : reader.GetString(reader.GetOrdinal("IsApproved"))
        };
    }
}


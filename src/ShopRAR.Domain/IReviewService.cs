using System.Collections.Generic;

namespace ShopRAR.Domain;

public interface IReviewService
{
    PagedResult<Review> GetAllReviewsPaged(int pageNumber, int pageSize);
    Review? GetReviewById(int id);
    Review CreateReview(Review review);
    void UpdateReview(Review review);
    void DeleteReview(int id);
    IEnumerable<Review> GetReviewsByProduct(int productId);
    IEnumerable<Review> GetApprovedReviews(int productId);
    void ApproveReview(int reviewId);
    void RejectReview(int reviewId);
}


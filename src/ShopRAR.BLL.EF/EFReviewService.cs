using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShopRAR.Domain;

namespace ShopRAR.BLL.EF;

public class EfReviewService : IReviewService
{
    private readonly AppDbContext _context;

    public EfReviewService(AppDbContext context)
    {
        _context = context;
    }

    public PagedResult<Review> GetAllReviewsPaged(int pageNumber, int pageSize)
    {
        var totalCount = _context.Reviews.Count();
        
        var items = _context.Reviews
            .OrderBy(r => r.ReviewId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Review>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public Review? GetReviewById(int id)
    {
        return _context.Reviews.FirstOrDefault(r => r.ReviewId == id);
    }

    public Review CreateReview(Review review)
    {
        if (review.ReviewId == 0)
        {
            var maxId = _context.Reviews.Any() 
                ? _context.Reviews.Max(r => r.ReviewId) 
                : 0;
            review.ReviewId = maxId + 1;
        }
        
        _context.Reviews.Add(review);
        _context.SaveChanges();
        return review;
    }

    public void UpdateReview(Review review)
    {
        var existing = _context.Reviews.FirstOrDefault(r => r.ReviewId == review.ReviewId);
        if (existing == null) return;

        existing.Rating = review.Rating;
        existing.Comments = review.Comments;
        existing.IsApproved = review.IsApproved;

        _context.SaveChanges();
    }

    public void DeleteReview(int id)
    {
        var exists = _context.Reviews
            .AsNoTracking()
            .Any(r => r.ReviewId == id);
        
        if (!exists)
        {
            throw new InvalidOperationException("Review does not exist.");
        }

        var trackedEntity = _context.Reviews.Local.FirstOrDefault(r => r.ReviewId == id);
        
        if (trackedEntity != null)
        {
            _context.Reviews.Remove(trackedEntity);
        }
        else
        {
            var reviewToDelete = new Review { ReviewId = id };
            _context.Entry(reviewToDelete).State = EntityState.Deleted;
        }
        
        _context.SaveChanges();
    }

    public IEnumerable<Review> GetReviewsByProduct(int productId)
    {
        return _context.Reviews
            .Where(r => r.ProductId == productId)
            .OrderBy(r => r.ReviewId)
            .ToList();
    }

    public IEnumerable<Review> GetApprovedReviews(int productId)
    {
        return _context.Reviews
            .Where(r => r.ProductId == productId && r.IsApproved == "True")
            .OrderBy(r => r.ReviewId)
            .ToList();
    }

    public void ApproveReview(int reviewId)
    {
        var existing = _context.Reviews.FirstOrDefault(r => r.ReviewId == reviewId);
        if (existing == null) return;

        existing.IsApproved = "True";
        _context.SaveChanges();
    }

    public void RejectReview(int reviewId)
    {
        var existing = _context.Reviews.FirstOrDefault(r => r.ReviewId == reviewId);
        if (existing == null) return;

        existing.IsApproved = "False";
        _context.SaveChanges();
    }
}


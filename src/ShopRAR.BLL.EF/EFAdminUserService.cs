using System.Linq;
using ShopRAR.Domain;

namespace ShopRAR.BLL.EF;

public class EfAdminUserService : IAdminUserService
{
    private readonly AppDbContext _context;

    public EfAdminUserService(AppDbContext context)
    {
        _context = context;
    }

    public AdminUser? AuthenticateAdmin(string email, string passwordHash)
    {
        return _context.AdminUsers
            .FirstOrDefault(a => 
                a.Email != null && 
                a.Email.Trim().ToLower() == email.Trim().ToLower() &&
                a.PasswordHash != null &&
                a.PasswordHash.Trim() == passwordHash.Trim());
    }

    public AdminUser? GetAdminById(int id)
    {
        return _context.AdminUsers.FirstOrDefault(a => a.AdminUserId == id);
    }

    public AdminUser CreateAdmin(string email, string passwordHash)
    {
        var maxId = _context.AdminUsers.Any() 
            ? _context.AdminUsers.Max(a => a.AdminUserId) 
            : 0;

        var admin = new AdminUser
        {
            AdminUserId = maxId + 1,
            Email = email,
            PasswordHash = passwordHash
        };

        _context.AdminUsers.Add(admin);
        _context.SaveChanges();
        return admin;
    }

    public void UpdateAdmin(int adminId, string email, string? passwordHash = null)
    {
        var existing = _context.AdminUsers.FirstOrDefault(a => a.AdminUserId == adminId);
        if (existing == null) return;

        existing.Email = email;
        if (passwordHash != null)
        {
            existing.PasswordHash = passwordHash;
        }

        _context.SaveChanges();
    }
}


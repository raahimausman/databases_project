namespace ShopRAR.Domain;

public interface IAdminUserService
{
    AdminUser? AuthenticateAdmin(string email, string passwordHash);
    AdminUser? GetAdminById(int id);
    AdminUser CreateAdmin(string email, string passwordHash);
    void UpdateAdmin(int adminId, string email, string? passwordHash = null);
}


using System.Collections.Generic;

namespace ShopRAR.Domain;

public interface ICustomerService
{
    IEnumerable<Customer> GetAllCustomers();
    PagedResult<Customer> GetAllCustomersPaged(int pageNumber, int pageSize);
    Customer? GetCustomerById(int id);
    PagedResult<Customer> SearchCustomersByNamePaged(string searchTerm, int pageNumber, int pageSize);
    Customer CreateCustomer(Customer customer);
    void UpdateCustomer(Customer customer);
    void DeleteCustomer(int id);
}

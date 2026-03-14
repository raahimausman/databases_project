using System.Collections.Generic;
using System.Linq;
using ShopRAR.Domain;

namespace ShopRAR.BLL.EF;

public class EfCustomerService : ICustomerService
{
    private readonly AppDbContext _context;

    public EfCustomerService(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Customer> GetAllCustomers()
    {
        return _context.Customers
            .OrderBy(c => c.CustomerId)
            .ToList();
    }

    public PagedResult<Customer> GetAllCustomersPaged(int pageNumber, int pageSize)
    {
        var totalCount = _context.Customers.Count();
        
        var items = _context.Customers
            .OrderBy(c => c.CustomerId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Customer>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public Customer? GetCustomerById(int id)
    {
        return _context.Customers.FirstOrDefault(c => c.CustomerId == id);
    }

    public PagedResult<Customer> SearchCustomersByNamePaged(string searchTerm, int pageNumber, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new PagedResult<Customer>
            {
                Items = Enumerable.Empty<Customer>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        var term = searchTerm.ToLower();
        var query = _context.Customers
            .Where(c => c.FullName != null && c.FullName.ToLower().Contains(term));

        var totalCount = query.Count();

        var items = query
            .OrderBy(c => c.CustomerId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Customer>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public Customer CreateCustomer(Customer customer)
    {
        if (customer.CustomerId == 0)
        {
            var maxId = _context.Customers.Any() 
                ? _context.Customers.Max(c => c.CustomerId) 
                : 0;
            customer.CustomerId = maxId + 1;
        }
        
        _context.Customers.Add(customer);
        _context.SaveChanges();
        return customer;
    }

    public void UpdateCustomer(Customer customer)
    {
        var existing = _context.Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
        if (existing == null) return;

        existing.FullName     = customer.FullName;
        existing.Email        = customer.Email;
        existing.Phone        = customer.Phone;
        existing.AddressLine  = customer.AddressLine;
        existing.City         = customer.City;

        _context.SaveChanges();
    }

    public void DeleteCustomer(int id)
    {
        var existing = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
        if (existing == null)
        {
            throw new InvalidOperationException("Customer does not exist.");
        }

        var orderCount = _context.Orders.Count(o => o.CustomerId == id);
        if (orderCount > 0)
        {
            throw new InvalidOperationException($"Cannot delete customer because they have {orderCount} existing order(s). Please delete or reassign orders first.");
        }

        var reviewCount = _context.Reviews.Count(r => r.CustomerId == id);
        if (reviewCount > 0)
        {
            throw new InvalidOperationException($"Cannot delete customer because they have {reviewCount} existing review(s). Please delete reviews first.");
        }

        _context.Customers.Remove(existing);
        _context.SaveChanges();
    }
}

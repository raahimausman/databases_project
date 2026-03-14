using System.Collections.Generic;

namespace ShopRAR.Domain;

public interface IReportingService
{
    
    IEnumerable<TopSellingProduct> GetTopSellingProducts(int year, int month, int topN = 10);
    PagedResult<CustomerLifetimeSpend> GetCustomerLifetimeSpendPaged(int pageNumber, int pageSize);
}


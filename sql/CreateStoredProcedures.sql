USE ShopRAR;
GO

--get order total func
CREATE OR ALTER FUNCTION fn_GetOrderTotal (@OrderID INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Total DECIMAL(18,2);

    SELECT @Total = SUM(LineTotalAmount)
    FROM OrderItems
    WHERE OrderId = @OrderID;

    RETURN ISNULL(@Total,0);
END;
GO


--create order proc
CREATE OR ALTER PROCEDURE usp_CreateOrder
    @CustomerId  INT,
    @AdminUserId INT,
    @Status      NVARCHAR(50) = 'Pending',
    @NewOrderId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Customer WHERE CustomerId = @CustomerId)
    BEGIN
        RAISERROR('Cannot create order. Customer with ID %d does not exist.', 16, 1, @CustomerId);
        RETURN;
    END
    
    IF NOT EXISTS (SELECT 1 FROM AdminUser WHERE AdminUserId = @AdminUserId)
    BEGIN
        RAISERROR('Cannot create order. Admin user with ID %d does not exist.', 16, 1, @AdminUserId);
        RETURN;
    END

    IF @Status IS NULL OR LTRIM(RTRIM(@Status)) = ''
    BEGIN
        SET @Status = 'Pending';
    END

    DECLARE @NextOrderId INT;
    SELECT @NextOrderId = ISNULL(MAX(OrderId), 0) + 1
    FROM Orders;

    INSERT INTO Orders
        (OrderId, OrderDate, Status, TotalAmount, CustomerId, AdminUserId)
    VALUES
        (@NextOrderId, GETDATE(), @Status, 0, @CustomerId, @AdminUserId);

    SET @NewOrderId = @NextOrderId;
END;
GO

--add order item proc
CREATE OR ALTER PROCEDURE usp_AddOrderItem
    @OrderId INT,
    @ProductId INT,
    @Quantity INT,
    @UnitPriceAtOrder DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = @OrderId)
    BEGIN
        RAISERROR('Cannot add item. Order with ID %d does not exist.', 16, 1, @OrderId);
        RETURN;
    END
    
    IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductId = @ProductId)
    BEGIN
        RAISERROR('Cannot add item. Product with ID %d does not exist.', 16, 1, @ProductId);
        RETURN;
    END
    
    IF @Quantity <= 0
    BEGIN
        RAISERROR('Cannot add item. Quantity must be greater than zero.', 16, 1);
        RETURN;
    END
    
    DECLARE @AvailableStock INT;
    SELECT @AvailableStock = ISNULL(QuantityOnHand, 0) FROM Inventory WHERE ProductId = @ProductId;
    
    IF @AvailableStock < @Quantity
    BEGIN
        RAISERROR('Cannot add item. Insufficient stock. Available: %d, Requested: %d', 16, 1, @AvailableStock, @Quantity);
        RETURN;
    END

    DECLARE @LineTotal DECIMAL(18,2);
    DECLARE @NextOrderItemId INT;
    DECLARE @NewOrderTotal DECIMAL(18,2);

    SET @LineTotal = @Quantity * @UnitPriceAtOrder;

    SELECT @NextOrderItemId = ISNULL(MAX(OrderItemId),0) +1
    FROM OrderItems;

    INSERT INTO OrderItems
        (OrderItemId, OrderId, ProductId, UnitPriceAtOrder, Quantity, LineTotalAmount)
    VALUES
        (@NextOrderItemId, @OrderId, @ProductId, @UnitPriceAtOrder, @Quantity, @LineTotal);

    UPDATE Inventory
    SET QuantityOnHand = QuantityOnHand - @Quantity
    WHERE ProductId = @ProductId;

    SELECT @NewOrderTotal = ISNULL(SUM(LineTotalAmount), 0)
    FROM OrderItems
    WHERE OrderId = @OrderId;

    UPDATE Orders
    SET TotalAmount = @NewOrderTotal
    WHERE OrderId = @OrderId;
END;
GO

--get all customers proc
CREATE OR ALTER PROCEDURE usp_GetAllCustomers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CustomerId, FullName, Email, Phone, AddressLine, City
    FROM Customer;
END;
GO

--get all customers with pagination proc
CREATE OR ALTER PROCEDURE usp_GetAllCustomersPaged
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT @TotalCount = COUNT(*)
    FROM Customer;
    
    SELECT CustomerId, FullName, Email, Phone, AddressLine, City
    FROM Customer
    ORDER BY CustomerId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

--get customer by ID proc
CREATE OR ALTER PROCEDURE usp_GetCustomerById
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CustomerId, FullName, Email, Phone, AddressLine, City
    FROM Customer
    WHERE CustomerId = @CustomerId;
END;
GO

--search customers by name with pagination proc
CREATE OR ALTER PROCEDURE usp_SearchCustomersByNamePaged
    @SearchTerm NVARCHAR(255),
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalCount = COUNT(*)
    FROM Customer
    WHERE FullName LIKE '%' + @SearchTerm + '%';
    
    SELECT CustomerId, FullName, Email, Phone, AddressLine, City
    FROM Customer
    WHERE FullName LIKE '%' + @SearchTerm + '%'
    ORDER BY CustomerId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

--create customer proc
CREATE OR ALTER PROCEDURE usp_CreateCustomer
    @FullName NVARCHAR(255),
    @Email NVARCHAR(255),
    @Phone NVARCHAR(50),
    @AddressLine NVARCHAR(255),
    @City NVARCHAR(255),
    @NewCustomerId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NextId INT;
    SELECT @NextId = ISNULL(MAX(CustomerId), 0) + 1 FROM Customer;
    
    INSERT INTO Customer (CustomerId, FullName, Email, Phone, AddressLine, City)
    VALUES (@NextId, @FullName, @Email, @Phone, @AddressLine, @City);
    
    SET @NewCustomerId = @NextId;
END;
GO

--update customer proc
CREATE OR ALTER PROCEDURE usp_UpdateCustomer
    @CustomerId INT,
    @FullName NVARCHAR(255),
    @Email NVARCHAR(255),
    @Phone NVARCHAR(50),
    @AddressLine NVARCHAR(255),
    @City NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Customer WHERE CustomerId = @CustomerId)
    BEGIN
        RAISERROR('Cannot update customer. Customer with ID %d does not exist.', 16, 1, @CustomerId);
        RETURN;
    END
    
    UPDATE Customer
    SET FullName = @FullName, Email = @Email, Phone = @Phone,
        AddressLine = @AddressLine, City = @City
    WHERE CustomerId = @CustomerId;
END;
GO

--delete customer proc
CREATE OR ALTER PROCEDURE usp_DeleteCustomer
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Customer WHERE CustomerId = @CustomerId)
    BEGIN
        RAISERROR('Customer does not exist.', 16, 1);
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM Orders WHERE CustomerId = @CustomerId)
    BEGIN
        DECLARE @OrderCount INT;
        SELECT @OrderCount = COUNT(*) FROM Orders WHERE CustomerId = @CustomerId;
        RAISERROR('Cannot delete customer because they have %d existing order(s). Please delete or reassign orders first.', 16, 1, @OrderCount);
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM Review WHERE CustomerId = @CustomerId)
    BEGIN
        DECLARE @ReviewCount INT;
        SELECT @ReviewCount = COUNT(*) FROM Review WHERE CustomerId = @CustomerId;
        RAISERROR('Cannot delete customer because they have %d existing review(s). Please delete reviews first.', 16, 1, @ReviewCount);
        RETURN;
    END
    
    DELETE FROM Customer WHERE CustomerId = @CustomerId;
END;
GO

--get all products proc
CREATE OR ALTER PROCEDURE usp_GetAllProducts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ProductId, Name, SKU, Price, Description, IsActive
    FROM Product;
END;
GO

--get all products with pagination proc
CREATE OR ALTER PROCEDURE usp_GetAllProductsPaged
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT @TotalCount = COUNT(*)
    FROM Product;
 
    SELECT ProductId, Name, SKU, Price, Description, IsActive
    FROM Product
    ORDER BY ProductId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

--get product by ID proc
CREATE OR ALTER PROCEDURE usp_GetProductById
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ProductId, Name, SKU, Price, Description, IsActive
    FROM Product
    WHERE ProductId = @ProductId;
END;
GO

--create product proc
CREATE OR ALTER PROCEDURE usp_CreateProduct
    @Name NVARCHAR(255),
    @SKU NVARCHAR(50),
    @Price DECIMAL(18,2),
    @Description NVARCHAR(MAX),
    @IsActive NVARCHAR(10),
    @NewProductId INT OUTPUT,
    @NewSKU NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @SKU IS NULL OR LTRIM(RTRIM(@SKU)) = ''
    BEGIN
        DECLARE @MaxSkuNumber INT = 19999; 
        DECLARE @CurrentSku NVARCHAR(50);
        DECLARE @SkuNumber INT;
        
        DECLARE sku_cursor CURSOR FOR
        SELECT SKU FROM Product WHERE SKU IS NOT NULL;
        
        OPEN sku_cursor;
        FETCH NEXT FROM sku_cursor INTO @CurrentSku;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN

            IF @CurrentSku LIKE 'SKU-[0-9][0-9][0-9][0-9][0-9][0-9]'
            BEGIN
                SET @SkuNumber = CAST(SUBSTRING(@CurrentSku, 5, 6) AS INT);
                IF @SkuNumber > @MaxSkuNumber
                BEGIN
                    SET @MaxSkuNumber = @SkuNumber;
                END
            END
            
            FETCH NEXT FROM sku_cursor INTO @CurrentSku;
        END
        
        CLOSE sku_cursor;
        DEALLOCATE sku_cursor;
        
        SET @SkuNumber = @MaxSkuNumber + 1;
        SET @SKU = 'SKU-' + RIGHT('000000' + CAST(@SkuNumber AS NVARCHAR(6)), 6);
    END
    
    IF EXISTS (SELECT 1 FROM Product WHERE LTRIM(RTRIM(SKU)) = LTRIM(RTRIM(@SKU)))
    BEGIN
        RAISERROR('A product with SKU ''%s'' already exists. SKU must be unique.', 16, 1, @SKU);
        RETURN;
    END
    
    DECLARE @NextId INT;
    SELECT @NextId = ISNULL(MAX(ProductId), 0) + 1 FROM Product;
    
    INSERT INTO Product (ProductId, Name, SKU, Price, Description, IsActive)
    VALUES (@NextId, @Name, @SKU, @Price, @Description, @IsActive);
    
    SET @NewProductId = @NextId;
    SET @NewSKU = @SKU;
END;
GO

--update product proc
CREATE OR ALTER PROCEDURE usp_UpdateProduct
    @ProductId INT,
    @Name NVARCHAR(255),
    @SKU NVARCHAR(50),
    @Price DECIMAL(18,2),
    @Description NVARCHAR(MAX),
    @IsActive NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductId = @ProductId)
    BEGIN
        RAISERROR('Cannot update product. Product with ID %d does not exist.', 16, 1, @ProductId);
        RETURN;
    END
    
    UPDATE Product
    SET Name = @Name, Price = @Price,
        Description = @Description, IsActive = @IsActive
    WHERE ProductId = @ProductId;
END;
GO

--delete product proc
CREATE OR ALTER PROCEDURE usp_DeleteProduct
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductId = @ProductId)
    BEGIN
        RAISERROR('Product does not exist.', 16, 1);
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM OrderItems WHERE ProductId = @ProductId)
    BEGIN
        DECLARE @OrderItemCount INT;
        SELECT @OrderItemCount = COUNT(*) FROM OrderItems WHERE ProductId = @ProductId;
        RAISERROR('Cannot delete product because it is referenced in %d order item(s). Products with order history cannot be deleted.', 16, 1, @OrderItemCount);
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM Review WHERE ProductId = @ProductId)
    BEGIN
        DECLARE @ReviewCount INT;
        SELECT @ReviewCount = COUNT(*) FROM Review WHERE ProductId = @ProductId;
        RAISERROR('Cannot delete product because it has %d review(s). Please delete reviews first.', 16, 1, @ReviewCount);
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM ProductCategory WHERE ProductId = @ProductId)
    BEGIN
        DELETE FROM ProductCategory WHERE ProductId = @ProductId;
    END
 
    IF EXISTS (SELECT 1 FROM Inventory WHERE ProductId = @ProductId)
    BEGIN
        DELETE FROM Inventory WHERE ProductId = @ProductId;
    END
  
    DELETE FROM Product WHERE ProductId = @ProductId;
END;
GO

--get active products with pagination proc
CREATE OR ALTER PROCEDURE usp_GetActiveProductsPaged
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT @TotalCount = COUNT(*)
    FROM Product
    WHERE IsActive = 'True';
   
    SELECT ProductId, Name, SKU, Price, Description, IsActive
    FROM Product
    WHERE IsActive = 'True'
    ORDER BY ProductId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

--get products by category with pagination proc
CREATE OR ALTER PROCEDURE usp_GetProductsByCategoryPaged
    @CategoryId INT,
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT @TotalCount = COUNT(DISTINCT p.ProductId)
    FROM Product p
    INNER JOIN ProductCategory pc ON p.ProductId = pc.ProductId
    WHERE pc.CategoryId = @CategoryId;
    
    SELECT p.ProductId, p.Name, p.SKU, p.Price, p.Description, p.IsActive
    FROM Product p
    INNER JOIN ProductCategory pc ON p.ProductId = pc.ProductId
    WHERE pc.CategoryId = @CategoryId
    ORDER BY p.ProductId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

--search products with pagination proc
CREATE OR ALTER PROCEDURE usp_SearchProductsPaged
    @SearchTerm NVARCHAR(255),
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalCount = COUNT(*)
    FROM Product
    WHERE Name LIKE '%' + @SearchTerm + '%';
    
    SELECT ProductId, Name, SKU, Price, Description, IsActive
    FROM Product
    WHERE Name LIKE '%' + @SearchTerm + '%'
    ORDER BY ProductId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

--get all categories proc
CREATE OR ALTER PROCEDURE usp_GetAllCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, Name, IsActive FROM Category;
END;
GO

--get category by ID proc
CREATE OR ALTER PROCEDURE usp_GetCategoryById
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, Name, IsActive FROM Category WHERE CategoryId = @CategoryId;
END;
GO

--create category proc
CREATE OR ALTER PROCEDURE usp_CreateCategory
    @Name NVARCHAR(255),
    @IsActive NVARCHAR(10),
    @NewCategoryId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NextId INT;
    SELECT @NextId = ISNULL(MAX(CategoryId), 0) + 1 FROM Category;
    
    INSERT INTO Category (CategoryId, Name, IsActive)
    VALUES (@NextId, @Name, @IsActive);
    
    SET @NewCategoryId = @NextId;
END;
GO

--update category proc
CREATE OR ALTER PROCEDURE usp_UpdateCategory
    @CategoryId INT,
    @Name NVARCHAR(255),
    @IsActive NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Category WHERE CategoryId = @CategoryId)
    BEGIN
        RAISERROR('Cannot update category. Category with ID %d does not exist.', 16, 1, @CategoryId);
        RETURN;
    END
    
    UPDATE Category SET Name = @Name, IsActive = @IsActive
    WHERE CategoryId = @CategoryId;
END;
GO

--delete category proc
CREATE OR ALTER PROCEDURE usp_DeleteCategory
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Category WHERE CategoryId = @CategoryId)
    BEGIN
        RAISERROR('Category does not exist.', 16, 1);
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM ProductCategory WHERE CategoryId = @CategoryId)
    BEGIN
        DECLARE @ProductCount INT;
        SELECT @ProductCount = COUNT(DISTINCT ProductId) FROM ProductCategory WHERE CategoryId = @CategoryId;
        RAISERROR('Cannot delete category because it is associated with %d product(s). Please remove product associations first.', 16, 1, @ProductCount);
        RETURN;
    END
    
    DELETE FROM Category WHERE CategoryId = @CategoryId;
END;
GO

--get active categories proc
CREATE OR ALTER PROCEDURE usp_GetActiveCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, Name, IsActive FROM Category WHERE IsActive = 'True';
END;
GO

--get all reviews with pagination proc
CREATE OR ALTER PROCEDURE usp_GetAllReviewsPaged
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT @TotalCount = COUNT(*)
    FROM Review;
    
    SELECT ReviewId, ProductId, CustomerId, Rating, Comments, IsApproved
    FROM Review
    ORDER BY ReviewId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

--review by ID proc
CREATE OR ALTER PROCEDURE usp_GetReviewById
    @ReviewId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ReviewId, ProductId, CustomerId, Rating, Comments, IsApproved
    FROM Review WHERE ReviewId = @ReviewId;
END;
GO

--create review proc
CREATE OR ALTER PROCEDURE usp_CreateReview
    @ProductId INT,
    @CustomerId INT,
    @Rating INT,
    @Comments NVARCHAR(MAX),
    @IsApproved NVARCHAR(10),
    @NewReviewId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductId = @ProductId)
    BEGIN
        RAISERROR('Cannot create review. Product with ID %d does not exist.', 16, 1, @ProductId);
        RETURN;
    END
    
    IF NOT EXISTS (SELECT 1 FROM Customer WHERE CustomerId = @CustomerId)
    BEGIN
        RAISERROR('Cannot create review. Customer with ID %d does not exist.', 16, 1, @CustomerId);
        RETURN;
    END

    IF @Rating < 1 OR @Rating > 5
    BEGIN
        RAISERROR('Cannot create review. Rating must be between 1 and 5.', 16, 1);
        RETURN;
    END
    
    DECLARE @NextId INT;
    SELECT @NextId = ISNULL(MAX(ReviewId), 0) + 1 FROM Review;
    
    INSERT INTO Review (ReviewId, ProductId, CustomerId, Rating, Comments, IsApproved)
    VALUES (@NextId, @ProductId, @CustomerId, @Rating, @Comments, @IsApproved);
    
    SET @NewReviewId = @NextId;
END;
GO

--update review proc
CREATE OR ALTER PROCEDURE usp_UpdateReview
    @ReviewId INT,
    @Rating INT,
    @Comments NVARCHAR(MAX),
    @IsApproved NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
   
    IF NOT EXISTS (SELECT 1 FROM Review WHERE ReviewId = @ReviewId)
    BEGIN
        RAISERROR('Cannot update review. Review with ID %d does not exist.', 16, 1, @ReviewId);
        RETURN;
    END
  
    IF @Rating < 1 OR @Rating > 5
    BEGIN
        RAISERROR('Cannot update review. Rating must be between 1 and 5.', 16, 1);
        RETURN;
    END
    
    UPDATE Review
    SET Rating = @Rating, Comments = @Comments, IsApproved = @IsApproved
    WHERE ReviewId = @ReviewId;
END;
GO

--delete review proc
CREATE OR ALTER PROCEDURE usp_DeleteReview
    @ReviewId INT
AS
BEGIN
    SET NOCOUNT ON;
   
    IF NOT EXISTS (SELECT 1 FROM Review WHERE ReviewId = @ReviewId)
    BEGIN
        RAISERROR('Cannot delete review. Review with ID %d does not exist.', 16, 1, @ReviewId);
        RETURN;
    END
    
    DELETE FROM Review WHERE ReviewId = @ReviewId;
END;
GO

--get reviews by product proc
CREATE OR ALTER PROCEDURE usp_GetReviewsByProduct
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ReviewId, ProductId, CustomerId, Rating, Comments, IsApproved
    FROM Review WHERE ProductId = @ProductId;
END;
GO

--get approved reviews proc
CREATE OR ALTER PROCEDURE usp_GetApprovedReviews
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ReviewId, ProductId, CustomerId, Rating, Comments, IsApproved
    FROM Review WHERE ProductId = @ProductId AND IsApproved = 'True';
END;
GO

--approve review proc
CREATE OR ALTER PROCEDURE usp_ApproveReview
    @ReviewId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Review WHERE ReviewId = @ReviewId)
    BEGIN
        RAISERROR('Cannot approve review. Review with ID %d does not exist.', 16, 1, @ReviewId);
        RETURN;
    END
    
    UPDATE Review SET IsApproved = 'True' WHERE ReviewId = @ReviewId;
END;
GO

--reject review proc
CREATE OR ALTER PROCEDURE usp_RejectReview
    @ReviewId INT
AS
BEGIN
    SET NOCOUNT ON;
   
    IF NOT EXISTS (SELECT 1 FROM Review WHERE ReviewId = @ReviewId)
    BEGIN
        RAISERROR('Cannot reject review. Review with ID %d does not exist.', 16, 1, @ReviewId);
        RETURN;
    END
    
    UPDATE Review SET IsApproved = 'False' WHERE ReviewId = @ReviewId;
END;
GO

--get inventory by product ID proc
CREATE OR ALTER PROCEDURE usp_GetInventoryByProductId
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.ProductId, i.QuantityOnHand, p.Name as ProductName
    FROM Inventory i
    INNER JOIN Product p ON i.ProductId = p.ProductId
    WHERE i.ProductId = @ProductId;
END;
GO

--update inventory proc
CREATE OR ALTER PROCEDURE usp_UpdateInventory
    @ProductId INT,
    @Quantity INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductId = @ProductId)
    BEGIN
        RAISERROR('Cannot update inventory. Product with ID %d does not exist.', 16, 1, @ProductId);
        RETURN;
    END
    
    IF @Quantity < 0
    BEGIN
        RAISERROR('Cannot update inventory. Quantity cannot be negative.', 16, 1);
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM Inventory WHERE ProductId = @ProductId)
    BEGIN
        UPDATE Inventory SET QuantityOnHand = @Quantity WHERE ProductId = @ProductId;
    END
    ELSE
    BEGIN
        INSERT INTO Inventory (ProductId, QuantityOnHand) VALUES (@ProductId, @Quantity);
    END
END;
GO

--adjust inventory proc
CREATE OR ALTER PROCEDURE usp_AdjustInventory
    @ProductId INT,
    @QuantityChange INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductId = @ProductId)
    BEGIN
        RAISERROR('Cannot adjust inventory. Product with ID %d does not exist.', 16, 1, @ProductId);
        RETURN;
    END
    
    IF NOT EXISTS (SELECT 1 FROM Inventory WHERE ProductId = @ProductId)
    BEGIN
        RAISERROR('Cannot adjust inventory. Inventory record for product ID %d does not exist. Use UpdateInventory to create it.', 16, 1, @ProductId);
        RETURN;
    END
    
    DECLARE @CurrentQuantity INT;
    SELECT @CurrentQuantity = QuantityOnHand FROM Inventory WHERE ProductId = @ProductId;
    
    IF (@CurrentQuantity + @QuantityChange) < 0
    BEGIN
        RAISERROR('Cannot adjust inventory. Adjustment would result in negative quantity. Current: %d, Change: %d', 16, 1, @CurrentQuantity, @QuantityChange);
        RETURN;
    END
    
    UPDATE Inventory
    SET QuantityOnHand = QuantityOnHand + @QuantityChange
    WHERE ProductId = @ProductId;
END;
GO

--get stock quantity proc
CREATE OR ALTER PROCEDURE usp_GetStockQuantity
    @ProductId INT,
    @Quantity INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT @Quantity = ISNULL(QuantityOnHand, 0) FROM Inventory WHERE ProductId = @ProductId;
END;
GO

--get low stock products with pagination proc
CREATE OR ALTER PROCEDURE usp_GetLowStockProductsPaged
    @Threshold INT,
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
   
    SELECT @TotalCount = COUNT(*)
    FROM Inventory i
    WHERE i.QuantityOnHand < @Threshold;
    
    SELECT i.ProductId, i.QuantityOnHand
    FROM Inventory i
    WHERE i.QuantityOnHand < @Threshold
    ORDER BY i.QuantityOnHand ASC, i.ProductId ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

--get order by ID proc
CREATE OR ALTER PROCEDURE usp_GetOrderById
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.OrderId, o.OrderDate, o.Status, o.TotalAmount, o.CustomerId, o.AdminUserId,
           c.FullName as CustomerName
    FROM Orders o
    LEFT JOIN Customer c ON o.CustomerId = c.CustomerId
    WHERE o.OrderId = @OrderId;
END;
GO

--update order status proc
CREATE OR ALTER PROCEDURE usp_UpdateOrderStatus
    @OrderId INT,
    @Status NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = @OrderId)
    BEGIN
        RAISERROR('Cannot update order status. Order with ID %d does not exist.', 16, 1, @OrderId);
        RETURN;
    END
    
    UPDATE Orders SET Status = @Status WHERE OrderId = @OrderId;
END;
GO

--cancel order proc
CREATE OR ALTER PROCEDURE usp_CancelOrder
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = @OrderId)
    BEGIN
        RAISERROR('Cannot cancel order. Order with ID %d does not exist.', 16, 1, @OrderId);
        RETURN;
    END
    
    DELETE FROM Orders WHERE OrderId = @OrderId;
END;
GO

--get orders by customer proc
CREATE OR ALTER PROCEDURE usp_GetOrdersByCustomer
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.OrderId, o.OrderDate, o.Status, o.TotalAmount, o.CustomerId, o.AdminUserId,
           c.FullName as CustomerName
    FROM Orders o
    LEFT JOIN Customer c ON o.CustomerId = c.CustomerId
    WHERE o.CustomerId = @CustomerId
    ORDER BY o.OrderDate DESC;
END;
GO

--get order items proc
CREATE OR ALTER PROCEDURE usp_GetOrderItems
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT OrderItemId, OrderId, ProductId, UnitPriceAtOrder, Quantity, LineTotalAmount
    FROM OrderItems WHERE OrderId = @OrderId;
END;
GO

--get order summary by ID proc
CREATE OR ALTER PROCEDURE usp_GetOrderSummaryById
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT OrderId, OrderDate, Status, StoredTotalAmount, CalculatedTotalAmount,
           CustomerId, CustomerName, CustomerEmail, AdminUserId, AdminEmail
    FROM v_OrderSummary
    WHERE OrderId = @OrderId;
END;
GO

-- get all order summaries with pagination proc
CREATE OR ALTER PROCEDURE usp_GetAllOrderSummariesPaged
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT @TotalCount = COUNT(*)
    FROM v_OrderSummary;
    
    SELECT OrderId, OrderDate, Status, StoredTotalAmount, CalculatedTotalAmount,
           CustomerId, CustomerName, CustomerEmail, AdminUserId, AdminEmail
    FROM v_OrderSummary
    ORDER BY OrderDate DESC, OrderId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

--authenticate admin proc
CREATE OR ALTER PROCEDURE usp_AuthenticateAdmin
    @Email NVARCHAR(255),
    @PasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AdminUserId, Email, PasswordHash
    FROM AdminUser
    WHERE LOWER(LTRIM(RTRIM(Email))) = LOWER(LTRIM(RTRIM(@Email)))
      AND LTRIM(RTRIM(PasswordHash)) = LTRIM(RTRIM(@PasswordHash));
END;
GO

--get admin by ID
CREATE OR ALTER PROCEDURE usp_GetAdminById
    @AdminId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AdminUserId, Email, PasswordHash
    FROM AdminUser
    WHERE AdminUserId = @AdminId;
END;
GO

--create Admin
CREATE OR ALTER PROCEDURE usp_CreateAdmin
    @Email NVARCHAR(255),
    @PasswordHash NVARCHAR(255),
    @NewAdminId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM AdminUser WHERE LOWER(LTRIM(RTRIM(Email))) = LOWER(LTRIM(RTRIM(@Email))))
    BEGIN
        RAISERROR('An admin with this email already exists.', 16, 1);
        RETURN;
    END

    DECLARE @NextAdminId INT;
    SELECT @NextAdminId = ISNULL(MAX(AdminUserId), 0) + 1
    FROM AdminUser;

    INSERT INTO AdminUser (AdminUserId, Email, PasswordHash)
    VALUES (@NextAdminId, @Email, @PasswordHash);

    SET @NewAdminId = @NextAdminId;
END;
GO

--update admin proc
CREATE OR ALTER PROCEDURE usp_UpdateAdmin
    @AdminId INT,
    @Email NVARCHAR(255),
    @PasswordHash NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM AdminUser WHERE AdminUserId = @AdminId)
    BEGIN
        RAISERROR('Admin with ID %d does not exist.', 16, 1, @AdminId);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM AdminUser WHERE AdminUserId != @AdminId 
               AND LOWER(LTRIM(RTRIM(Email))) = LOWER(LTRIM(RTRIM(@Email))))
    BEGIN
        RAISERROR('An admin with this email already exists.', 16, 1);
        RETURN;
    END

    IF @PasswordHash IS NOT NULL
    BEGIN
        UPDATE AdminUser
        SET Email = @Email,
            PasswordHash = @PasswordHash
        WHERE AdminUserId = @AdminId;
    END
    ELSE
    BEGIN
        UPDATE AdminUser
        SET Email = @Email
        WHERE AdminUserId = @AdminId;
    END
END;
GO


--get top selling products for a specific month/year proc
CREATE OR ALTER PROCEDURE usp_GetTopSellingProducts
    @Year INT,
    @Month INT,
    @TopN INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    ;WITH
        cte_MonthOrders
        AS
        (
            SELECT OrderId
            FROM Orders
            WHERE YEAR(OrderDate) = @Year
                AND MONTH(OrderDate) = @Month
                AND Status <> 'Cancelled'
        ),
        cte_ProductSales
        AS
        (
            SELECT ProductId,
                SUM(Quantity) AS TotalQty,
                SUM(LineTotalAmount) AS TotalRevenue
            FROM OrderItems
            WHERE OrderId IN (SELECT OrderId FROM cte_MonthOrders)
            GROUP BY ProductId
        )
    SELECT TOP(@TopN)
        p.ProductId,
        p.Name AS ProductName,
        ps.TotalQty,
        ps.TotalRevenue
    FROM cte_ProductSales ps
        INNER JOIN Product p ON p.ProductId = ps.ProductId
    ORDER BY ps.TotalQty DESC;
END;
GO

--get customer lifetime spend with pagination proc
CREATE OR ALTER PROCEDURE usp_GetCustomerLifetimeSpendPaged
    @PageNumber INT,
    @PageSize INT,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
 
    ;WITH
        cte_CustomerSpend
        AS
        (
            SELECT CustomerId
            FROM Orders
            WHERE Status <> 'Cancelled'
            GROUP BY CustomerId
        )
    SELECT @TotalCount = COUNT(*)
    FROM cte_CustomerSpend cs
        INNER JOIN Customer c ON cs.CustomerId = c.CustomerId;

    ;WITH
        cte_CustomerSpend
        AS
        (
            SELECT CustomerId,
                SUM(TotalAmount) AS LifetimeSpend,
                COUNT(OrderId) AS OrderCount,
                MAX(OrderDate) AS LastOrderDate
            FROM Orders
            WHERE Status <> 'Cancelled'
            GROUP BY CustomerId
        )
    SELECT
        cs.CustomerId,
        c.FullName,
        cs.LifetimeSpend,
        cs.OrderCount,
        cs.LastOrderDate
    FROM cte_CustomerSpend cs
        INNER JOIN Customer c ON cs.CustomerId = c.CustomerId
    ORDER BY cs.LifetimeSpend DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

PRINT 'All stored procedures created successfully!';
GO

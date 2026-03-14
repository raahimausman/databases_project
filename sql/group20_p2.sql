--creating db
IF DB_ID('ShopRAR') IS NOT NULL
BEGIN
    ALTER DATABASE ShopRAR SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ShopRAR;
END
GO

CREATE DATABASE ShopRAR;
GO

USE ShopRAR;
GO

--admin user table
CREATE TABLE dbo.AdminUser
(
    AdminUserId INT PRIMARY KEY,
    Email NVARCHAR(255),
    PasswordHash NVARCHAR(255)
);
GO

--customer table
CREATE TABLE dbo.Customer
(
    CustomerId INT PRIMARY KEY,
    FullName NVARCHAR(255),
    Email NVARCHAR(255),
    Phone NVARCHAR(50),
    AddressLine NVARCHAR(255),
    City NVARCHAR(255)
);
GO

--category table
CREATE TABLE dbo.Category
(
    CategoryId INT PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    IsActive NVARCHAR(10)
);
GO

--product table
CREATE TABLE dbo.Product
(
    ProductId INT PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    SKU NVARCHAR(50),
    Price DECIMAL(18,2),
    Description NVARCHAR(MAX),
    IsActive NVARCHAR(10)
);
GO

--inventory table
CREATE TABLE dbo.Inventory
(
    ProductId INT PRIMARY KEY,
    QuantityOnHand INT,
    CONSTRAINT FK_Inventory_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Product(ProductId)
);
GO

--orders table
CREATE TABLE dbo.Orders
(
    OrderId INT NOT NULL,
    OrderDate DATETIME,
    Status NVARCHAR(50),
    TotalAmount DECIMAL(18,2),
    CustomerId INT,
    AdminUserId INT,
    CONSTRAINT PK_Orders PRIMARY KEY NONCLUSTERED (OrderId),
    CONSTRAINT FK_Orders_Customer FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customer(CustomerId),
    CONSTRAINT FK_Orders_AdminUser FOREIGN KEY (AdminUserId)
        REFERENCES dbo.AdminUser(AdminUserId)
);
GO

--order items table
CREATE TABLE dbo.OrderItems
(
    OrderItemId INT PRIMARY KEY,
    OrderId INT,
    ProductId INT,
    UnitPriceAtOrder DECIMAL(18,2),
    Quantity INT,
    LineTotalAmount DECIMAL(18,2),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId)
        REFERENCES dbo.Orders(OrderId),
    CONSTRAINT FK_OrderItems_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Product(ProductId)
);
GO

--product category table
CREATE TABLE dbo.ProductCategory
(
    ProductId INT,
    CategoryId INT,
    CONSTRAINT PK_ProductCategory PRIMARY KEY (ProductId, CategoryId),
    CONSTRAINT FK_PC_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Product(ProductId),
    CONSTRAINT FK_PC_Category FOREIGN KEY (CategoryId)
        REFERENCES dbo.Category(CategoryId)
);
GO

--review table
CREATE TABLE dbo.Review
(
    ReviewId INT PRIMARY KEY,
    ProductId INT,
    CustomerId INT,
    Rating INT,
    Comments NVARCHAR(MAX),
    IsApproved NVARCHAR(10),
    CONSTRAINT FK_Review_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Product(ProductId),
    CONSTRAINT FK_Review_Customer FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customer(CustomerId)
);
GO


--data loading
BULK INSERT dbo.AdminUser
FROM '/var/opt/mssql/data/AdminUser.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    TABLOCK
);
GO

BULK INSERT dbo.Customer
FROM '/var/opt/mssql/data/Customer.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    TABLOCK
);
GO

BULK INSERT dbo.Category
FROM '/var/opt/mssql/data/Category.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    TABLOCK
);
GO

BULK INSERT dbo.Product
FROM '/var/opt/mssql/data/Product.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    TABLOCK
);
GO

BULK INSERT dbo.Inventory
FROM '/var/opt/mssql/data/Inventory.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    TABLOCK
);
GO

BULK INSERT dbo.Orders
FROM '/var/opt/mssql/data/Orders.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    TABLOCK
);
GO

BULK INSERT dbo.ProductCategory
FROM '/var/opt/mssql/data/ProductCategory.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    TABLOCK
);
GO

BULK INSERT dbo.OrderItems
FROM '/var/opt/mssql/data/OrderItem.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    TABLOCK
);
GO

BULK INSERT dbo.Review
FROM '/var/opt/mssql/data/Review.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    TABLOCK
);
GO

--create order proc
CREATE OR ALTER PROCEDURE usp_CreateOrder
    @CustomerId  INT,
    @AdminUserId INT,
    @NewOrderId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NextOrderId INT;
    SELECT @NextOrderId = ISNULL(MAX(OrderId), 0) + 1
    FROM Orders;

    INSERT INTO Orders
        (OrderId, OrderDate, Status, TotalAmount, CustomerId, AdminUserId)
    VALUES
        (@NextOrderId, GETDATE(), 'Pending', 0, @CustomerId, @AdminUserId);

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

    DECLARE @LineTotal DECIMAL(18,2);
    DECLARE @NextOrderItemId INT;

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
    @NewProductId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NextId INT;
    SELECT @NextId = ISNULL(MAX(ProductId), 0) + 1 FROM Product;
    
    INSERT INTO Product (ProductId, Name, SKU, Price, Description, IsActive)
    VALUES (@NextId, @Name, @SKU, @Price, @Description, @IsActive);
    
    SET @NewProductId = @NextId;
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
    UPDATE Product
    SET Name = @Name, SKU = @SKU, Price = @Price,
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
    DELETE FROM Product WHERE ProductId = @ProductId;
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

--get review by ID proc
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
    UPDATE Review SET IsApproved = 'True' WHERE ReviewId = @ReviewId;
END;
GO

--get inventory by product ID proc
CREATE OR ALTER PROCEDURE usp_GetInventoryByProductId
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ProductId, QuantityOnHand FROM Inventory WHERE ProductId = @ProductId;
END;
GO

--update inventory proc
CREATE OR ALTER PROCEDURE usp_UpdateInventory
    @ProductId INT,
    @Quantity INT
AS
BEGIN
    SET NOCOUNT ON;
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

--get order by ID proc
CREATE OR ALTER PROCEDURE usp_GetOrderById
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT OrderId, OrderDate, Status, TotalAmount, CustomerId, AdminUserId
    FROM Orders WHERE OrderId = @OrderId;
END;
GO

--update order status proc
CREATE OR ALTER PROCEDURE usp_UpdateOrderStatus
    @OrderId INT,
    @Status NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Orders SET Status = @Status WHERE OrderId = @OrderId;
END;
GO

--cancel order proc
CREATE OR ALTER PROCEDURE usp_CancelOrder
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Orders WHERE OrderId = @OrderId;
END;
GO

--get orders by customer proc
CREATE OR ALTER PROCEDURE usp_GetOrdersByCustomer
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT OrderId, OrderDate, Status, TotalAmount, CustomerId, AdminUserId
    FROM Orders
    WHERE CustomerId = @CustomerId
    ORDER BY OrderDate DESC;
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

--AFTER trigger to keep Orders.TotalAmount in sync
CREATE OR ALTER TRIGGER trg_OrderItems_AfterChange_UpdateOrderTotal
ON OrderItems
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    ;
    WITH
        ChangedOrders
        AS
        (
            SELECT OrderId
            FROM inserted
            UNION
                SELECT OrderId
                FROM deleted
        )
    UPDATE o
    SET o.TotalAmount = ISNULL(s.SumLineTotal, 0)
    FROM Orders o
        INNER JOIN (
        SELECT oi.OrderId, SUM(oi.LineTotalAmount) AS SumLineTotal
        FROM OrderItems oi
        GROUP BY oi.OrderId
    ) s ON o.OrderId = s.OrderId
    WHERE o.OrderId IN (SELECT OrderId
    FROM ChangedOrders);
END;
GO

--INSTEAD OF DELETE trigger to delete orders
CREATE OR ALTER TRIGGER trg_Orders_InsteadOfDelete_SoftDelete
ON Orders
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE o
    SET o.Status = 'Cancelled'
    FROM Orders o
        INNER JOIN deleted d
        ON o.OrderId = d.OrderId;
END;
GO

--get order total function
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

--is product in stock function
CREATE OR ALTER FUNCTION fn_IsProductInStock (@ProductID INT)
RETURNS BIT
AS
BEGIN
    DECLARE @Qty INT;

    SELECT @Qty = QuantityOnHand
    FROM Inventory
    WHERE ProductId = @ProductID;

    IF (@Qty IS NULL OR @Qty <= 0)
        RETURN 0;

    RETURN 1;
END;
GO

-- active product catalog view
CREATE OR ALTER VIEW v_ActiveProductCatalog
AS
    SELECT
        p.ProductId,
        p.Name AS ProductName,
        p.SKU,
        p.Price,
        p.IsActive AS ProductIsActive,
        c.CategoryId,
        c.Name AS CategoryName,
        c.IsActive AS CategoryIsActive,
        i.QuantityOnHand,
        dbo.fn_IsProductInStock(p.ProductId) AS IsInStock
    FROM Product p
        LEFT JOIN ProductCategory pc ON p.ProductId  = pc.ProductId
        LEFT JOIN Category c ON pc.CategoryId = c.CategoryId
        LEFT JOIN Inventory i ON p.ProductId  = i.ProductId
    WHERE p.IsActive = 'True';
GO

--order summary view
CREATE OR ALTER VIEW v_OrderSummary
AS
    SELECT
        o.OrderId,
        o.OrderDate,
        o.Status,
        o.TotalAmount AS StoredTotalAmount,
        dbo.fn_GetOrderTotal(o.OrderId) AS CalculatedTotalAmount,

        c.CustomerId,
        c.FullName AS CustomerName,
        c.Email AS CustomerEmail,

        a.AdminUserId,
        a.Email AS AdminEmail
    FROM Orders o
        INNER JOIN Customer  c ON o.CustomerId  = c.CustomerId
        LEFT JOIN AdminUser a ON o.AdminUserId = a.AdminUserId;
GO

-- top selling products CTE
DECLARE @ReportYear INT = YEAR(GETDATE()), 
        @ReportMonth INT = MONTH(GETDATE()),
        @TopN INT = 10;

;WITH
    cte_MonthOrders
    AS
    (
        SELECT OrderId
        FROM Orders
        WHERE YEAR(OrderDate) = @ReportYear
            AND MONTH(OrderDate) = @ReportMonth
            AND Status <> 'Cancelled'
    ),
    cte_ProductSales
    AS
    (
        SELECT ProductId,
            SUM(Quantity) AS TotalQty,
            SUM(LineTotalAmount) AS TotalRevenue
        FROM OrderItems
        WHERE OrderId IN (SELECT OrderId
        FROM cte_MonthOrders)
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
GO

--customer lifetime spend CTE
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
ORDER BY cs.LifetimeSpend DESC;
GO

--product lookup by category index
IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name = 'idx_ProductCategory_Category'
)
BEGIN
    CREATE NONCLUSTERED INDEX idx_ProductCategory_Category
    ON ProductCategory (CategoryId)
    INCLUDE (ProductId);
END
GO

--low stock alerts filtered index
IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name = 'idx_Inventory_LowStock'
)
BEGIN
    CREATE NONCLUSTERED INDEX idx_Inventory_LowStock
    ON Inventory (QuantityOnHand)
    WHERE QuantityOnHand <= 5;
END
GO

--speed filtering orders by customer & date index
IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name = 'idx_Orders_Customer_OrderDate'
)
BEGIN
    CREATE NONCLUSTERED INDEX idx_Orders_Customer_OrderDate
    ON Orders (CustomerId, OrderDate DESC)
    INCLUDE (Status,TotalAmount);
END
GO

-- order tables by month partition
CREATE PARTITION FUNCTION pf_OrdersByMonth (DATETIME)
AS RANGE RIGHT FOR VALUES (
    '2024-02-01', '2024-03-01', '2024-04-01', '2024-05-01', '2024-06-01',
    '2024-07-01', '2024-08-01', '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
    '2025-02-01', '2025-03-01', '2025-04-01', '2025-05-01', '2025-06-01',
    '2025-07-01', '2025-08-01', '2025-09-01', '2025-10-01', '2025-11-01', '2025-12-01',
    '2026-01-01'
);
GO

CREATE PARTITION SCHEME ps_OrdersByMonth
AS PARTITION pf_OrdersByMonth
ALL TO ([PRIMARY]);
GO

CREATE CLUSTERED INDEX CIX_Orders_OrderDate
ON dbo.Orders (OrderDate)
ON ps_OrdersByMonth(OrderDate);
GO

-- order by status partition
CREATE PARTITION FUNCTION pf_OrdersByStatus (NVARCHAR(50))
AS RANGE LEFT FOR VALUES ('Cancelled', 'Pending', 'Delivered', 'Confirmed');
GO

CREATE PARTITION SCHEME ps_OrdersByStatus
AS PARTITION pf_OrdersByStatus
ALL TO ([PRIMARY]);
GO

CREATE NONCLUSTERED INDEX IX_Orders_Status_Partitioned
ON dbo.Orders (Status)
ON ps_OrdersByStatus(Status);
GO
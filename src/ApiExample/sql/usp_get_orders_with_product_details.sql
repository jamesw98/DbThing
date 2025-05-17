ALTER PROCEDURE [dbo].[usp_get_orders_with_product_details]
AS
	
	SELECT
		S.SalesOrderID,
		S.ProductID,
		S.CarrierTrackingNumber,
		P.[Name], 
		P.ProductNumber,
		P.Color,
		P.ListPrice
	FROM Sales.SalesOrderDetail S
	INNER JOIN Production.Product P
		ON S.ProductID = P.ProductID

RETURN 0 
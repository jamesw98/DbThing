CREATE PROCEDURE [dbo].[usp_get_employees]
AS

    SELECT
        E.BusinessEntityID,
        E.HireDate,
        E.JobTitle,
        P.FirstName,
        P.LastName,
        P.Title
    FROM HumanResources.Employee E
    INNER JOIN Person.Person P
        ON E.BusinessEntityID = P.BusinessEntityID

RETURN 0
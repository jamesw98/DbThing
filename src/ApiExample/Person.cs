using Attributes;
using DbThing.Common.Interfaces;

namespace ApiExample;

public partial class Person : IDbPreProcessModel
{
    [DbColumn("BusinessEntityID", Required = true)]
    public int PersonId { get; set; }
    
    [DbColumn("HireDate", Required = true)]
    public DateTime HireDate { get; set; }

    [DbColumn("FirstName", Required = true)]
    public string FirstName { get; set; } = string.Empty;
    
    [DbColumn("LastName", Required = true)]
    public string LastName { get; set; } = string.Empty;
    
    [DbColumn("Title")]
    public string? Title { get; set; }
}
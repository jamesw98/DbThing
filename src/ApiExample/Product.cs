using Attributes;
using DbThing.Common.Interfaces;

namespace ApiExample;

public partial class Product : IDbPreProcessModel
{
    [DbColumn("Name")]
    public string Name { get; set; } = string.Empty;

    [DbColumn("ProductNumber")]
    public string ProductNumber { get; set; } = string.Empty;
    
    [DbColumn("Color")] 
    public string Color { get; set; } = string.Empty;
    
    [DbColumn("ListPrice")]
    public decimal Price { get; set; }  
}
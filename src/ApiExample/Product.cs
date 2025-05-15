using Attributes;
using DbThing;

namespace ApiExample;

public partial class Product : IDbPreProcessModel, IDbModel
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
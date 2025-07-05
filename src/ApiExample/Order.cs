using Attributes;
using DbThing.Common.Interfaces;

namespace ApiExample;

public partial class Order : IDbPreProcessModel
{
    [DbColumn("SalesOrderID")]
    public int OrderId { get; set; }
    
    [DbColumn("ProductID")]
    public int ProductId { get; set; }

    [DbColumn("CarrierTrackingNumber")] 
    public string TrackingNumber { get; set; } = string.Empty;

    [DbComplexColumn]
    public Product Product { get; set; } = new();
}
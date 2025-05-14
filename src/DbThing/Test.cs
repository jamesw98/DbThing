using Attributes;

namespace DbThing;

public partial class Test : IDbPreProcessModel
{
    [DbColumn("NAME", Required=true)]
    public string Name { get; set; }
    
    [DbColumn("ID")]
    public long Id { get; set; }
}
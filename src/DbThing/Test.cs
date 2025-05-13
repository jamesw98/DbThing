using jamesw98.Attributes;

namespace DbThing;

public partial class Test : IDbPreProcessModel
{
    [DbColumn<string>(columnName:"NAME")]
    public string Name { get; set; }
}
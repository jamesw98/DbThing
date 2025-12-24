using DbThing.Attributes;
using DbThing.Interfaces;

namespace ConsoleExample;

public partial class User : IDbPreProcessModel
{
    [DbColumn("USER_ID")]
    public long Id { get; set; }
    [DbColumn("USER_NAME")]
    public string UserName { get; set; } = string.Empty;    
    [DbColumn("CREATED_DATE")]
    public string CreatedDate { get; set; } = string.Empty;
}
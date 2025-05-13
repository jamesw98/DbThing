namespace jamesw98.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DbColumnAttribute<T> : Attribute
{
    public DbColumnAttribute(string columnName)
    {
        ColumnName = columnName;
    }
    
    public string ColumnName { get; set; } = string.Empty;
}
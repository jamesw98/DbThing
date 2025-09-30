namespace DbThing.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DbColumnAttribute(string columnName) : Attribute
{
    /// <summary>
    /// Name of the column to get.
    /// </summary>
    public string ColumnName { get; set; } = columnName;

    /// <summary>
    /// Whether this column is required, defaults to false. 
    /// </summary>
    public bool Required { get; set; }
}
namespace DbThing;

public interface IDbModel
{
    /// <summary>
    /// Initialize the model with data from the database.
    /// </summary>
    /// <param name="values">A dictionary of raw values from the database. The key being the column name.</param>
    public void Initialize(Dictionary<string, object> values);
}
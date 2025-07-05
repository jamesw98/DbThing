namespace DbThing.Common.Interfaces;

public interface IDbModel
{
    /// <summary>
    /// Initialize the model with data from the database.
    /// </summary>
    /// <param name="values">A dictionary of raw values from the database. The key being the column name.</param>
    public void Initialize(Dictionary<string, object> values)
    {
        throw new NotImplementedException("\"Initialize(Dictionary<string, object> values)\" not implemented for this class. Either implement it yourself or use the DbThingGenerator: https://github.com/jamesw98/DbThing/tree/main/src/DbThingGenerator");
    }
}
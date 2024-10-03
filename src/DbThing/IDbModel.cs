namespace DbThing;

public interface IDbModel
{
    public void Initialize(Dictionary<string, object> values);
}
using System.Data;

namespace DbThing;

public static class ExtensionMethods
{
    public static T TryGet<T>(this Dictionary<string, object> dict, string key) 
    {
        if (!dict.TryGetValue(key, out var foundVal))
        {
            throw new DataException($"Could not column \"{key}\" in database response.");
        }

        return TryCast<T>(foundVal);
    }

    public static T TryCast<T>(this object obj)
    {
        try
        {
            // Attempt to cast the found value to T.
            return (T)obj;
        }
        catch (InvalidCastException e)
        {
            // Throw a more human readable exception.
            throw new InvalidCastException($"Could not cast value to {typeof(T)}.", e);
        }
    }
}
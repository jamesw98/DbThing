using System;
using System.Data;

namespace DbThing;

public static class ExtensionMethods
{
    /// <summary>
    /// Attempts to get a required field from the database response.
    /// This should be used when you're expecting a value to not be null. If a value could be null from the database
    /// use <see cref="TryGet{T}"/> instead.
    /// </summary>
    /// <param name="dict">The object dictionary to search through.</param>
    /// <param name="key">The key to attempt to find a value for.</param>
    /// <typeparam name="T">The type the value should attemp to be cast into.</typeparam>
    /// <returns>A value from the database cast to type T.</returns>
    /// <exception cref="DataException">
    /// If the value could not be found in the database response, the value could not be parsed into type T, or the
    /// value ended up being null after being cast.
    /// </exception>
    public static T TryGetRequired<T>(this Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var foundVal))
        {
            throw new DataException($"Could not column \"{key}\" in database response.");
        }

        return TryCast<T?>(foundVal) 
               ?? throw new DataException($"Could not find non-null value for \"{key}\"");
    }
    
    /// <summary>
    /// Attempts to get a field from the database response.
    /// </summary>
    /// <param name="dict">The object dictionary to search through.</param>
    /// <param name="key">The key to attempt to find a value for.</param>
    /// <typeparam name="T">The type the value should attemp to be cast into.</typeparam>
    /// <returns>A value from the database cast to type T.</returns>
    /// <exception cref="DataException">
    /// If the value could not be found in the database response or the value could not be parsed into type T.
    /// </exception>
    public static T? TryGet<T>(this Dictionary<string, object> dict, string key) 
    {
        if (!dict.TryGetValue(key, out var foundVal))
        {
            throw new DataException($"Could not column \"{key}\" in database response.");
        }

        return TryCast<T?>(foundVal);
    }
    
    /// <summary>
    /// Attempts to cast the a found object into type T. 
    /// </summary>
    /// <param name="obj">The object to attempt to cast.</param>
    /// <typeparam name="T">The type to attempt to cast into.</typeparam>
    /// <returns>The cast value of the object.</returns>
    /// <exception cref="InvalidCastException">If we could not cast the value into type T.</exception>
    public static T? TryCast<T>(this object obj)
    {
        try
        {
            if (obj is DBNull)
            {
                return default;
            }
            return (T?)obj;
        }
        catch (InvalidCastException e)
        {
            // Throw a more human readable exception.
            throw new InvalidCastException($"Could not cast value to {typeof(T)}.", e);
        }
    }
}
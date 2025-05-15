using System.Data;
using Microsoft.Data.SqlClient;

namespace DbThing;

public static class AsyncSqlExtensions
{
    #region Async
    
    /// <summary>
    /// Query the database via stored procedure.
    /// </summary>
    /// <param name="connection">The connection to run the procedure against.</param>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="parameters">The parameters to pass to the procedure.</param>
    /// <param name="type">Command type for this query. (Defaults to <see cref="CommandType.StoredProcedure"/>)</param>
    /// <typeparam name="T">The type of object to map the procedure results to.</typeparam>
    /// <returns>A list of type <see cref="T"/>, mapped from the results of the stored procedure.</returns>
    public static async Task<List<T>> QueryAsync<T>
    (
        this SqlConnection connection,
        string procedure,
        SqlParameter[] parameters,
        CommandType type = CommandType.StoredProcedure 
    ) where T : IDbModel, new()
    {
        var (transaction, command) = Utils.Setup(connection, procedure, parameters, type);
        return await Utils.RunAsync(connection, transaction, command, procedure, async () =>
        {
            await using var reader = await command.ExecuteReaderAsync();

            var result = new List<T>();
            while (reader.Read())
            {
                var valuesForKeys = new Dictionary<string, object>();
                var values = new object[reader.FieldCount];
                reader.GetValues(values);

                for (var i = 0; i < values.Length; i++)
                { 
                    valuesForKeys.Add(reader.GetName(i), values[i]);
                }

                var item = new T();
                item.Initialize(valuesForKeys);
                result.Add(item);
            }

            return result;
        });
    }
    
    /// <summary>
    /// Query the database via stored procedure for a single column of data. 
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="columnName">The name of the column to get data for.</param>
    /// <param name="parameters">The parameters to pass to the procedure.</param>
    /// <param name="type">Command type for this query. (Defaults to <see cref="CommandType.StoredProcedure"/>)</param>
    /// <typeparam name="T">The expected return type of the result.</typeparam>
    /// <returns>A list of data representing a single column of a database result.</returns>
    public static async Task<List<T?>> QuerySingleColumnListAsync<T>
    (
        this SqlConnection connection,
        string procedure,
        string columnName,
        SqlParameter[] parameters,
        CommandType type = CommandType.StoredProcedure
    )
    {
        var (transaction, command) = Utils.Setup(connection, procedure, parameters, type);
        return await Utils.RunAsync(connection, transaction, command, procedure, async () =>
        {
            await using var reader = await command.ExecuteReaderAsync();
            
            var resultValues = new List<T?>();
            while (reader.Read())
            {
                try
                {
                    var ordinal = reader.GetOrdinal(columnName);
                    var result = reader.GetValue(ordinal);
                    resultValues.Add(result.TryCast<T>());
                }
                catch (IndexOutOfRangeException e)
                {
                    // Throw a more human-readable exception.
                    throw new IndexOutOfRangeException($"Could not find column {columnName} in procedure result", e);
                }
            }

            return resultValues;
        });
    }

    /// <summary>
    /// Gets a list of objects from the database using a custom action for parsing.
    /// </summary>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="procedure">The stored procedure to run.</param>
    /// <param name="action">The action used to parsing.</param>
    /// <param name="parameters">Any parameters to send to the procedure.</param>
    /// <param name="type">Command type for this query. (Defaults to <see cref="CommandType.StoredProcedure"/>)</param>
    /// <typeparam name="T">The expected return type for the list.</typeparam>
    /// <returns>A list of type T.</returns>
    public static async Task<List<T?>> GetListCustomAction<T>
    (
        this SqlConnection connection,
        string procedure,
        Func<Task<List<T?>>> action,
        SqlParameter[] parameters,
        CommandType type = CommandType.StoredProcedure
    )
    {
        var (transaction, command) = Utils.Setup(connection, procedure, parameters, type);
        return await Utils.RunAsync(connection, transaction, command, procedure, action);
    }

    /// <summary>
    /// Gets a single object from the database.
    /// </summary>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="procedure">The stored procedure to run.</param>
    /// <param name="parameters">Any parameter to send to the procedure.</param>
    /// <param name="type">Command type for this query. (Defaults to <see cref="CommandType.StoredProcedure"/>)</param>
    /// <typeparam name="T">THe expected return type for the single object.</typeparam>
    /// <returns>A single object of type T.</returns>
    public static async Task<T> GetSingleAsync<T>
    (
        this SqlConnection connection,
        string procedure,
        SqlParameter[] parameters,
        CommandType type = CommandType.StoredProcedure
    ) where T : IDbModel, new()
    {
        var result = await connection.QueryAsync<T>(procedure, parameters, type);
        return result.SingleOrDefault() ?? throw new DataException("No row found.");
    }
    
    #endregion
}
using System.Data;
using Microsoft.Data.SqlClient;

namespace DbThing;

public static class SqlExtensions
{
    #region Async
    
    /// <summary>
    /// Query the database via stored procedure.
    /// </summary>
    /// <param name="connection">The connection to run the procedure against.</param>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="parameters">The parameters to pass to the procedure.</param>
    /// <typeparam name="T">The type of object to map the procedure results to.</typeparam>
    /// <returns>A list of type <see cref="T"/>, mapped from the results of the stored procedure.</returns>
    public static async Task<List<T>> QueryAsync<T>
    (
        this SqlConnection connection,
        string procedure,
        params SqlParameter[] parameters
    ) where T : IDbModel, new()
    {
        var (transaction, command) = Setup(connection, procedure, parameters);
        return await RunAsync(connection, transaction, command, async () =>
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
    /// <typeparam name="T">The expected return type of the result.</typeparam>
    /// <returns>A list of data representing a single column of a database result.</returns>
    public static async Task<List<T?>> QuerySingleColumnAsync<T>
    (
        this SqlConnection connection,
        string procedure,
        string columnName,
        params SqlParameter[] parameters
    )
    {
        var (transaction, command) = Setup(connection, procedure, parameters);
        return await RunAsync(connection, transaction, command, async () =>
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
                    // Throw a more human readable exception.
                    throw new IndexOutOfRangeException($"Could not find column {columnName} in procedure result",
                        e);
                }
            }

            return resultValues;
        });
    }
    
    #endregion
        
    #region Sync
    
    /// <summary>
    /// Executes a stored procedure that is not expected to return any data.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="procedure">The procedure to be run.</param>
    /// <param name="parameters">The parameters to pass to the procedure.</param>
    public static void Execute
    (
        this SqlConnection connection,
        string procedure,
        params SqlParameter[] parameters
    )
    {
        var (transaction, command) = Setup(connection, procedure, parameters);
        Run(connection, transaction, command, () => command.ExecuteNonQuery());
    }
    
    /// <inheritdoc cref="QueryAsync{T}"/>
    public static List<T> Query<T>
    (
        this SqlConnection connection,
        string procedure,
        params SqlParameter[] parameters
    ) where T : IDbModel, new()
    {
        var (transaction, command) = Setup(connection, procedure, parameters);

        return Run(connection, transaction, command, () =>
        {
            using var reader = command.ExecuteReader();

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
    
    /// <inheritdoc cref="QuerySingleColumnAsync{T}"/>
    public static List<T?> QuerySingleColumn<T>
    (
        this SqlConnection connection,
        string procedure,
        string columnName,
        params SqlParameter[] parameters
    )
    {
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var command = new SqlCommand(procedure, connection, transaction);
        command.Parameters.AddRange(parameters);
        command.CommandType = CommandType.StoredProcedure;

        using var reader = command.ExecuteReader();

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
                // Throw a more human readable exception.
                throw new IndexOutOfRangeException($"Could not find column {columnName} in procedure result", e);
            }
        }
        
        transaction.Commit();
        connection.Close();
        return resultValues;
    }
    
    #endregion
    
    #region Private

    /// <summary>
    /// Sets up a query to be run.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="parameters">The parameters to send to the procedure.</param>
    /// <returns>A tuple containing a transaction and the sql command.</returns>
    private static (SqlTransaction transaction, SqlCommand command) Setup
    (
        SqlConnection connection, 
        string procedure,
        SqlParameter[] parameters
    )
    {
        connection.Open();
        var transaction = connection.BeginTransaction();
        var command = new SqlCommand(procedure, connection, transaction);

        foreach (var p in parameters)
        {
            // If we have a default value or a null value, convert it to DBNull. This prevents a bunch of strange things
            // from happening
            if (p.IsNullable && p.Value == default || p.Value is null)
            {
                p.SqlValue = DBNull.Value;
            }
        }

        
        command.Parameters.AddRange(parameters);
        command.CommandType = CommandType.StoredProcedure;
        return (transaction, command);
    }
    
    /// <summary>
    /// Run a stored procedure. This handles commiting or rolling back the transaction depending on the result of the
    /// query.
    /// </summary>
    /// <param name="connection">The DB connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="command">The command to run.</param>
    /// <param name="action">The action to run the command and do any parsing.</param>
    /// <typeparam name="T">The type we want to return from the procedure.</typeparam>
    /// <returns>The result of the procedure.</returns>
    private static async Task<T> RunAsync<T>
    (
        SqlConnection connection,
        SqlTransaction transaction,
        SqlCommand command,
        Func<Task<T>> action
    )
    {
        try
        {
            var result = await action.Invoke();
            transaction.Commit();
            return result;
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            command.Dispose();
            connection.Close();
        }
    }
    
    /// <inheritdoc cref="RunAsync{T}"/>
    private static T Run<T>
    (
        SqlConnection connection,
        SqlTransaction transaction,
        SqlCommand command,
        Func<T> action
    )
    {
        try
        {
            var result = action.Invoke();
            transaction.Commit();
            return result;
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            command.Dispose();
            connection.Close();
        }
    }
    
    #endregion
}
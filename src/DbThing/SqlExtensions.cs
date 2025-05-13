using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Serilog;

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
        return await RunAsync(connection, transaction, command, procedure, async () =>
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
    public static async Task<List<T?>> QuerySingleColumnListAsync<T>
    (
        this SqlConnection connection,
        string procedure,
        string columnName,
        params SqlParameter[] parameters
    )
    {
        var (transaction, command) = Setup(connection, procedure, parameters);
        return await RunAsync(connection, transaction, command, procedure, async () =>
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
    /// <typeparam name="T">The expected return type for the list.</typeparam>
    /// <returns>A list of type T.</returns>
    public static async Task<List<T?>> GetListCustomAction<T>
    (
        this SqlConnection connection,
        string procedure,
        Func<Task<List<T?>>> action,
        params SqlParameter[] parameters
    )
    {
        var (transaction, command) = Setup(connection, procedure, parameters);
        return await RunAsync(connection, transaction, command, procedure, action);
    }

    /// <summary>
    /// Gets a single object from the database.
    /// </summary>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="procedure">The stored procedure to run.</param>
    /// <param name="parameters">Any parameter to send to the procedure.</param>
    /// <typeparam name="T">THe expected return type for the single object.</typeparam>
    /// <returns>A single object of type T.</returns>
    public static async Task<T> GetSingleAsync<T>
    (
        this SqlConnection connection,
        string procedure,
        params SqlParameter[] parameters
    ) where T : IDbModel, new()
    {
        var result = await connection.QueryAsync<T>(procedure, parameters);
        return result.SingleOrDefault() ?? throw new DataException("No row found.");
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
        Run(connection, transaction, command, procedure, () => command.ExecuteNonQuery());
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

        return Run(connection, transaction, command, procedure,() =>
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
    
    /// <inheritdoc cref="QuerySingleColumnListAsync{T}"/>
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
                // Throw a more human-readable exception.
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
        // Open a connection, start a transaction, and create a new SQL command with the appropriate params.
        connection.Open();
        var transaction = connection.BeginTransaction();
        var command = new SqlCommand(procedure, connection, transaction);
        
        // Check all the procedure parameters to see if we have a default or a null value. If we do, convert the 
        // parameter to DBNull. This prevents a bunch of strange things from happening.
        foreach (var p in parameters)
        {
            if (p is { IsNullable: true, Value: null } || p.Value is null)
            {
                p.SqlValue = DBNull.Value;
            }
        }

        // Add the params and command type to the SQL command.        
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
    /// <param name="procedure">The name of the proc being run.</param>
    /// <param name="action">The action to run the command and do any parsing.</param>
    /// <typeparam name="T">The type we want to return from the procedure.</typeparam>
    /// <returns>The result of the procedure.</returns>
    private static async Task<T> RunAsync<T>
    (
        SqlConnection connection,
        SqlTransaction transaction,
        SqlCommand command,
        string procedure,
        Func<Task<T>> action
    )
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var result = await action.Invoke();
            transaction.Commit();
            sw.Stop();
            Log.Information("Executed procedure {Procedure}. Took {Time}ms", procedure, 
                sw.Elapsed.Milliseconds);
            return result;
        }
        catch (Exception e)
        {
            Log.Error("Procedure {Procedure} encountered an error:\n{Error}", procedure, e.Message);
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
        string procedure,
        Func<T> action
    )
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var result = action.Invoke();
            transaction.Commit();
            sw.Stop();
            Log.Information("Executed procedure {Procedure}. Took {Time}ms", procedure, 
                sw.Elapsed.Milliseconds);
            return result;
        }
        catch (Exception e)
        {
            Log.Error("Procedure {Procedure} encountered an error:\n{Error}",
                procedure, e.Message);
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
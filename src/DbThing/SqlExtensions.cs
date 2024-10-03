using System.Data;
using Microsoft.Data.SqlClient;

namespace DbThing;

public static class SqlExtensions
{
    #region Async
    
    /// <summary>
    /// Asynchronously query the database via stored procedure. Returns a list of type <see cref="T"/>.
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
    
    public static async Task<List<T>> QuerySingleColumnAsync<T>
    (
        this SqlConnection connection,
        string procedure,
        string columnName,
        params SqlParameter[] parameters
    )
    {
        connection.Open();

        await using var transaction = connection.BeginTransaction();
        await using var command = new SqlCommand(procedure, connection, transaction);
        command.Parameters.AddRange(parameters);
        command.CommandType = CommandType.StoredProcedure;

        await using var reader = await command.ExecuteReaderAsync();

        var resultValues = new List<T>();
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
    
    #region Sync

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
    
    /// <summary>
    /// Query the database via stored procedure. Returns a list of type <see cref="T"/>.
    /// </summary>
    /// <param name="connection">The connection to run the procedure against.</param>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="parameters">The parameters to pass to the procedure.</param>
    /// <typeparam name="T">The type of object to map the procedure results to.</typeparam>
    /// <returns>A list of type <see cref="T"/>, mapped from the results of the stored procedure.</returns>
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
    
    public static List<T> QuerySingleColumn<T>
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

        var resultValues = new List<T>();
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
        command.Parameters.AddRange(parameters);
        command.CommandType = CommandType.StoredProcedure;
        return (transaction, command);
    }

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
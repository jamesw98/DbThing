using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DbThing;

public static class Utils
{
    /// <summary>
    /// Sets up a query to be run.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="parameters">The parameters to send to the procedure.</param>
    /// <returns>A tuple containing a transaction and the sql command.</returns>
    public static (SqlTransaction transaction, SqlCommand command) Setup
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
    public static async Task<T> RunAsync<T>
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
    public static T Run<T>
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
}
using System.Data;
using Microsoft.Data.SqlClient;

namespace DbThing;

public static class SyncSqlExtensions
{
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
        var (transaction, command) = Utils.Setup(connection, procedure, parameters);
        Utils.Run(connection, transaction, command, procedure, () => command.ExecuteNonQuery());
    }
    
    /// <inheritdoc cref="AsyncSqlExtensions.QueryAsync{T}"/>
    public static List<T> Query<T>
    (
        this SqlConnection connection,
        string procedure,
        params SqlParameter[] parameters
    ) where T : IDbModel, new()
    {
        var (transaction, command) = Utils.Setup(connection, procedure, parameters);

        return Utils.Run(connection, transaction, command, procedure,() =>
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
    
    /// <inheritdoc cref="AsyncSqlExtensions.QuerySingleColumnListAsync{T}"/>
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
}
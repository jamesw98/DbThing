using System.Data;
using DbThing.Common.Interfaces;
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
    /// <param name="type">Command type for this query. (Defaults to <see cref="CommandType.StoredProcedure"/>)</param>
    public static void Execute
    (
        this SqlConnection connection,
        string procedure,
        SqlParameter[] parameters,
        CommandType type = CommandType.StoredProcedure
    )
    {
        var (transaction, command) = Utils.Setup(connection, procedure, parameters, type);
        Utils.Run(connection, transaction, command, procedure, () => command.ExecuteNonQuery());
    }
    
    /// <inheritdoc cref="AsyncSqlExtensions.QueryAsync{T}"/>
    public static List<T> Query<T>
    (
        this SqlConnection connection,
        string procedure,
        SqlParameter[] parameters,
        CommandType type = CommandType.StoredProcedure
    ) where T : IDbModel, new()
    {
        var (transaction, command) = Utils.Setup(connection, procedure, parameters, type);

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
        SqlParameter[] parameters,
        CommandType type = CommandType.StoredProcedure
    )
    {
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var command = new SqlCommand(procedure, connection, transaction);
        command.Parameters.AddRange(parameters);
        command.CommandType = type;

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
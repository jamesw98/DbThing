using System.Data;
using System.Data.Common;
using DbThing.Interfaces;
using Microsoft.Data.SqlClient;

namespace DbThing;

public static class DbConnectionExtensions
{
    public static List<T> Query<T>
    (
        this IDbConnection connection,
        string sql,
        IEnumerable<SqlParameter>? parameters = null,
        CommandType type = CommandType.StoredProcedure
    ) where T : IDbModel, new()
    {
        var (tran, cmd) = Utils.Setup(connection, sql, parameters, type);
        return Utils.Run(connection, tran, cmd, sql, () =>
        {
            var result = new List<T>();

            using var reader = cmd.ExecuteReader();
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
}
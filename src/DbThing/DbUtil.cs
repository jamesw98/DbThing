using System.Data;
using System.Reflection;
using System.Transactions;

namespace DbThing;
using Microsoft.Data.SqlClient;

public class DbUtil(string connectionString)
{
    private SqlConnection _connection = new(connectionString);

    public async Task<List<T>> QueryAsync<T>(string procedure, params SqlParameter[] parameters)
        where T : IDbModel, new()
    {
        _connection.Open();

        await using var transaction = _connection.BeginTransaction();
        await using var command = new SqlCommand(procedure, _connection, transaction);
        command.Parameters.AddRange(parameters);
        command.CommandType = CommandType.StoredProcedure;

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

        _connection.Close();
        return result;
    }
}
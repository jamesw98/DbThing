using Microsoft.Data.SqlClient;

namespace DbThing;

public class DbRepository
{
    private readonly SqlConnection _connection;

    public DbRepository()
    {
        var connString = Environment.GetEnvironmentVariable("DbConnectionString");
        _connection = new SqlConnection(connString);
    }
    
    public DbRepository(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
    }
    
    public DbRepository(SqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<T>> QueryAsync<T>(string procedure, params SqlParameter[] parameters) 
        where T : IDbModel, new()
    {
        return await _connection.QueryAsync<T>(procedure, parameters);
    }

    public async Task<List<T?>> QuerySingleColumnAsync<T>
    (
        string procedure,
        string columnName,
        params SqlParameter[] parameters
    )
    {
        return await _connection.QuerySingleColumnAsync<T>(procedure, columnName, parameters);
    }

    public void Execute(string procedure, params SqlParameter[] parameters)
    {
        _connection.Execute(procedure, parameters);
    }
}
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DbThing;

/// <summary>
/// Repository base class. Extend this to interact with a SQL database using DbThing's extension methods.
/// </summary>
public class DbRepository
{
    private readonly SqlConnection _connection;

    /// <summary>
    /// Simple constructor that uses a default environment
    /// </summary>
    [Obsolete("Initializing with an empty constructor should only be used if you if you already have your" +
              " database connection string as an environment variable with key \"DbConnectionString\".")]
    public DbRepository()
    {
        // Set up the connection. 
        var connString = Environment.GetEnvironmentVariable("DbConnectionString");
        _connection = new SqlConnection(connString);
    }
    
    /// <summary>
    /// Primary constructor. Creates a SQL connection using an environment variable whose key is specified in a
    /// configuration. 
    /// </summary>
    public DbRepository(IConfiguration config)
    {
        // Do some validation.
        var connectionStringKey = config.GetSection("DbThing:DbConnectionStringKey").Value;
        if (connectionStringKey is null)
        {
            throw new DataException("\"DbConnectionStringKey\" is missing from config.");
        }

        if (connectionStringKey.Trim() == string.Empty)
        {
            throw new DataException("\"DbConnectionStringKey\" is empty.");
        }
        
        // Set up the connection. 
        var connString = Environment.GetEnvironmentVariable(connectionStringKey);
        _connection = new SqlConnection(connString);
    }
    
    /// <summary>
    /// Simple constructor that takes a raw connection string for testing or for internal applications.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    public DbRepository(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
    }
    
    /// <summary>
    /// Simple constructor that takes a SqlConnection object for testing or for internal applications.
    /// </summary>
    /// <param name="connection">The SQL connection object to use.</param>
    public DbRepository(SqlConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Query the database for a list of objects.
    /// </summary>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="parameters">Parameters to be passed to the procedure.</param>
    /// <typeparam name="TOutput">The type of the output list.</typeparam>
    /// <returns>A list of type <see cref="TOutput"/>.</returns>
    public async Task<List<TOutput>> QueryAsync<TOutput>(string procedure, params SqlParameter[] parameters) where TOutput : IDbModel, new()
    {
        return await _connection.QueryAsync<TOutput>(procedure, parameters);
    }

    /// <summary>
    /// Query the database to get a single column of data. 
    /// </summary>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="columnName">The name of the column to get data from.</param>
    /// <param name="parameters">Parameters to be passed to the procedure.</param>
    /// <typeparam name="TOutput">The type of the output list.</typeparam>
    /// <returns>A list of type <see cref="TOutput"/> which is a single column from the database response.</returns>
    public async Task<List<TOutput?>> QuerySingleColumnListAsync<TOutput>
    (
        string procedure,
        string columnName,
        params SqlParameter[] parameters
    )
    {
        return await _connection.QuerySingleColumnListAsync<TOutput>(procedure, columnName, parameters);
    }

    /// <summary>
    /// Executes a procedure that does not expect output.
    /// </summary>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="parameters">Parameters to be passed to the procedure.</param>
    public void Execute(string procedure, params SqlParameter[] parameters)
    {
        _connection.Execute(procedure, parameters);
    }

    /// <summary>
    /// Query a single row of a database. 
    /// </summary>
    /// <param name="procedure">The procedure to run.</param>
    /// <param name="parameters">Parameters to be passed to the procedure.</param>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    /// <returns>A single instance of <see cref="TOutput"/>.</returns>
    /// <exception cref="InvalidOperationException">If more than one row was found.</exception>
    /// <exception cref="DataException">If no row was found.</exception>
    public async Task<TOutput> QuerySingle<TOutput>(string procedure, params SqlParameter[] parameters) where TOutput : IDbModel, new()
    {
        return await _connection.GetSingleAsync<TOutput>(procedure, parameters);
    }
}
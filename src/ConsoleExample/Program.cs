using System.Data;
using ConsoleExample;
using DbThing;
using Microsoft.Data.Sqlite;

var sqlite = new SqliteConnection("Data Source=../../../db/test.db");

var users = sqlite.Query<User>("select * from TBL_USERS", type: CommandType.Text);
foreach (var x in users)
{
    Console.WriteLine($"{x.Id} {x.UserName} {x.CreatedDate}");
}
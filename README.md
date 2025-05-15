# DbThing
## What is it?
It's a thing, for your database. More specifically, a thing that lets you query the database and map the results of the queries to C# objects.   
You can write you own mapping method or you can let the source generator write it for you.

## Quick Samples
### Models
#### Manual
This is a model that has it's mapping method manually written. 
```csharp
using DbThing;

namespace ApiExample;

public class PersonManual : IDbModel
{
    public int PersonId { get; set; }
    public DateTime HireDate { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Title { get; set; }
    
    public void Initialize(Dictionary<string, object> values)
    {
        PersonId = values.TryGetRequired<int>("BusinessEntityId");
        HireDate = values.TryGetRequired<DateTime>("HireDate");
        FirstName = values.TryGetRequired<string>("FirstName");
        LastName = values.TryGetRequired<string>("LastName");
        Title = values.TryGet<string>("Title");
    }
}
```
#### Source Generated
This is a model who will have it's mapping method (`Initialize` in the above example) generated at build time.
```csharp
using Attributes;
using DbThing;

namespace ApiExample;

public partial class Person : IDbPreProcessModel, IDbModel
{
    [DbColumn("BusinessEntityID", Required = true)]
    public int PersonId { get; set; }
    
    [DbColumn("HireDate", Required = true)]
    public DateTime HireDate { get; set; }

    [DbColumn("FirstName", Required = true)]
    public string FirstName { get; set; } = string.Empty;
    
    [DbColumn("LastName", Required = true)]
    public string LastName { get; set; } = string.Empty;
    
    [DbColumn("Title")]
    public string? Title { get; set; }
}
```
### Querying
This example assumes you're building a minimal api, like in the example project included in this repo. 
```csharp
<.NET minimal api boilerplate>

var repo = var repo = new DbRepository(builder.Configuration);

app.MapGet("/person/{name}", ([FromRoute] string name) => {
    var result = repo.QueryAsync<Person>("usp_get_persons_whose_first_name_is", [new SqlParameter("@name", name)]);
    return result;
})
.WithName("FirstNames");
```

using ApiExample;
using DbThing;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var repo = new DbRepository(builder.Configuration);

app.MapGet("/person", async () =>
    {
        var results = await repo.QueryAsync<Person>("usp_get_employees");
        return results;
    })
.WithName("GetPeople");

app.Run();

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

app.MapGet("/fencer", async () =>
    {
        var results = await repo.QueryAsync<Fencer>("usp_get_fencers");
        return results;
    })
.WithName("GetFencers");

app.MapGet("/fencer/{id}", async () =>
    {
        var result = await repo.QuerySingle<Fencer>("usp_get_fencer", 
            new SqlParameter("@firstName", "Edoardo"),
            new SqlParameter("@lastName", "Mangiarotti"));
        return result;
    })
.WithName("GetFencerForId");

app.Run();

class Fencer : IDbModel
{
    public long FencerId { get; set; }
    public int FtFencerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int ClubId { get; set; }
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Initialize the model with values from the database.
    /// </summary>
    /// <param name="values">The raw response from the database.</param>
    public void Initialize(Dictionary<string, object> values)
    {
        FencerId = values.TryGet<long>("FENCER_ID");
        FtFencerId = values.TryGet<int>("FT_ID");
        FirstName = values.TryGetRequired<string>("FIRST_NAME");
        LastName = values.TryGetRequired<string>("LAST_NAME");
        ClubId = values.TryGetRequired<int>("CLUB_ID");
        Gender = values.TryGetRequired<string>("GENDER");
    }
}
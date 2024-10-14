# DbThing
## What is it?
It's a thing, for your database. More specifically, a thing that lets you query the database using stored procedures. 

## How to use the thing?
When you create a class to represent a response from your database/data model/whatever you want to call it, have it implement `IDbModel`. This will require you to implement the method `Initialize()` in your class. Once that has been implemented, you can use DbThing to call your stored procs and it will automatically attempt to parsse into whatever type you specified.  

See the examples below.

## Examples
A class representing an individual fencer.

```csharp
public class Fencer : IDbModel
{
    /// <summary>
    /// The ID of the fencer.
    /// </summary>
    public long FencerId { get; set; }
    
    /// <summary>
    /// First name of the fencer.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Last name of the fencer.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    ...

    /// <summary>
    /// The USA Fencing sabre rating for this fencer.
    /// This field defaults to "U" from the database.
    /// </summary>
    public string? UsaFencingSabreRating { get; set; }
    
    /// <summary>
    /// Initialize the model with data from the database.
    /// </summary>
    /// <param name="values">The values from the database to attempt to parse.</param>
    public void Initialize(Dictionary<string, object> values)
    {
        FencerId = values.TryGet<long>("FENCER_ID");
        FirstName = values.TryGetRequired<string>("FIRST_NAME");
        LastName = values.TryGetRequired<string>("LAST_NAME");
        MiddleName = values.TryGet<string>("MIDDLE_NAME");
        PrimaryClubId = values.TryGet<long>("PRIMARY_CLUB_ID");
        SecondaryClubId = values.TryGet<long>("SECONDARY_CLUB_ID");
        CreatedByUserId = values.TryGet<long>("CREATED_BY_USER_ID");
        IsPrivate = values.TryGet<bool>("IS_PRIVATE");
        UsaFencingId = values.TryGet<long>("USA_FENCING_ID");
        UsaFencingEpeeRating = values.TryGet<string>("USA_FENCING_EPEE_RATING");
        UsaFencingFoilRating = values.TryGet<string>("USA_FENCING_FOIL_RATING");
        UsaFencingSabreRating = values.TryGet<string>("USA_FENCING_SABRE_RATING");
    }
}

Repo method to get all fencers for a user.
```csharp
public async Task<List<Fencer>> GetAllFencersForUser(long requestingUserId)
{
    var fencers = await QueryAsync<Fencer>
    (
        "usp_get_fencers_for_user",
        new SqlParameter("@userId", requestingUserId)
    );
    return fencers;
}
```

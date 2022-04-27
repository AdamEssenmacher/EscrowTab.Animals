// "Minimal API" overview: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0

using EscrowTab.Animals;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

await EnsureDatabaseExistsAndIsBootstrappedWithRootNodeAsync();

// Add services to the container.
// This is where we'd set up DI.
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapGet("/api/tree", async () =>
{
    await using var db = new AnimalContext();

    // CTE enables 'infinite' tree depth without recursion
    // liberated from: https://khalidabuhakmeh.com/recursive-data-with-entity-framework-core-and-sql-server
    Animal root = (await db.Animals.FromSqlRaw(
            @"WITH animal (Id, ParentId, Label) AS (
                SELECT Id, ParentId, Label
                FROM Animals    
                WHERE ParentId is NULL
                UNION ALL
                SELECT a.Id, a.ParentId, a.Label
                FROM Animals a    
                INNER JOIN Animals a2 
                    ON a.ParentId = a2.Id
            )
            SELECT * FROM animal ORDER BY Id")

        // no tracking because we're read-only
        .AsNoTrackingWithIdentityResolution()

        // EF will interpret the CTE results as a list; we only need the first result (root)
        // We DO need EF to load all results, however, so this isn't a waste.
        .ToListAsync()).First();

    return new[] {root};
});

app.MapPost("/api/tree", async (PostDto data) =>
{
    await using var db = new AnimalContext();
    await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

    // Fail request if the requested parent does not exist
    Animal? parent = await db.Animals.FindAsync(data.Parent);
    if (parent == null)
        return Results.BadRequest($"Parent {data.Parent} does not exist.");

    // Save the new record.
    db.Animals.Add(new Animal
    {
        Label = data.Label,
        Parent = parent
    });
    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok();
});

app.MapDelete("/api/tree/{id}", async (long id) =>
{
    if (id == 1) // magic number, but I'll allow it.
        return Results.BadRequest("Cannot delete root.");

    await using var db = new AnimalContext();
    await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

    // Fail the request if the record has children.
    bool isParent = await db.Animals.AnyAsync(a => a.Parent != null && a.Parent.Id == id);
    if (isParent)
        return Results.BadRequest($"Animal {id} is a parent.");

    try
    {
        // Little trick to get EF to delete the record with one round-trip
        db.Remove(new Animal {Id = id});
        await db.SaveChangesAsync();
    }
    // thrown when attempting to delete a record that doesn't exist using the approach above
    catch (DbUpdateConcurrencyException)
    {
        return Results.BadRequest($"Animal {id} does not exist.");
    }

    await transaction.CommitAsync();
    return Results.Ok();
});

app.MapPut("/api/tree/{id}", async (long id, PutDto data) =>
{
    await using var db = new AnimalContext();
    try
    {
        // Changes the parent node in one round trip; might be a micro-optimization over non-SQL alternative
        int changed =
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE Animals SET ParentId = {0} WHERE Id = {1}", data.CurrentId, id);

        if (changed == 0)
            return Results.BadRequest($"Animal {id} does not exist.");
    }
    // Thrown when the attempted update would violate the FK constraint--meaning the parent id does not exist.
    catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
    {
        return Results.BadRequest($"Parent {data.CurrentId} does not exist.");
    }

    return Results.Ok();
});

app.Run();

static async Task EnsureDatabaseExistsAndIsBootstrappedWithRootNodeAsync()
{
    await using var context = new AnimalContext();
    context.Database.EnsureCreated();
    if (!await context.Animals.AnyAsync())
    {
        context.Animals.Add(new Animal {Label = "root"});
        await context.SaveChangesAsync();
    }
}
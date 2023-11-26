using BotApi.Services;
using BotApi.Types;
using ImageMagick;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI");
if (connectionString == null)
{
    connectionString = "mongodb://localhost:27017/";
}

var client = new MongoClient(connectionString);
var db = client.GetDatabase("iidx");

ConventionRegistry.Register(
    name: "CustomConventionPack",
    conventions: new ConventionPack
    {
        new CamelCaseElementNameConvention()
    },
    filter: _ => true
);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddSingleton<ResultRenderer>()
    .AddSingleton(db);

var app = builder
    .Build();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.CacheControl = $"max-age={24 * 60 * 60}";

    await next();
});

Task<byte[]> ShowError(ResultRenderer renderer, string error, string desc)
{
    return renderer.RenderUrl("file:///error.html", 500, 270, new {
        error, desc
    });
}

app.MapGet("/song/{id}.png", async (string id, ResultRenderer renderer, IMongoDatabase db, HttpContext ctx) =>
{
    var col = db.GetCollection<IIDXSong>("db");
    var cursor = await col.FindAsync(v => v.Id == id);
    var song = await cursor.FirstOrDefaultAsync();

    if (song != null)
    {
        var result = await renderer.RenderUrl("file:///song.html", 852, 293, song);

        ctx.Response.ContentType = "image/jpeg";
        await ctx.Response.Body.WriteAsync(result);

        return;
    }

    ctx.Response.ContentType = "image/jpeg";
    await ctx.Response.Body.WriteAsync(
        await ShowError(renderer, "未找到歌曲", $"数据库中不存在 ID 为 {id} 的歌曲")
    );
});

app.MapGet("/s/{keyword}/r.png", async (string keyword, ResultRenderer renderer, IMongoDatabase db, HttpContext ctx) =>
{
    var col = db.GetCollection<IIDXSong>("db");
    var cursor = await col.FindAsync(Builders<IIDXSong>.Filter.Text(keyword, new TextSearchOptions
    {
        CaseSensitive = false,
    }), new FindOptions<IIDXSong>
    {
        Limit = 10,
    });
    var songs = await cursor.ToListAsync();

    var result = await renderer.RenderUrl("file:///search.html", 580, 360, new
    {
        keyword,
        results = songs,
    });

    ctx.Response.ContentType = "image/jpeg";
    await ctx.Response.Body.WriteAsync(result);
});

app.MapGet("/about.png", async (ResultRenderer renderer, IMongoDatabase db, HttpContext ctx) =>
{
    var col = db.GetCollection<IIDXSong>("db");
    var count = await col.EstimatedDocumentCountAsync();
    var result = await renderer.RenderUrl("file:///about.html", 420, 220, new
    {
        count
    }, MagickFormat.Png);

    ctx.Response.ContentType = "image/png";
    await ctx.Response.Body.WriteAsync(result);
});

app.Run();

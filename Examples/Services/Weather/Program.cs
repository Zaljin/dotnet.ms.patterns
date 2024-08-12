using Weather;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.AddAPIDiscovery(o =>
{
    o.AddCrudAPI<CatAPIContract, CatAPIModel>();
    o.MakeAPIDiscoverable("weather", ["1.0"]);
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var catCrud = app.Services.GetRequiredService<ICrudAPIService<CatAPIModel>>();
var response = catCrud.GetById("test").Result;

app.Run();

public class CatAPIContract : APIContract
{
    public CatAPIContract() : base("cat", "1.0") { }
}

public class CatAPIModel
{

}
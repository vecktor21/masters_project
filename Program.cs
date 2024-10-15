using Diplom.Options;
using Diplom.Repositories;
using Diplom.Services;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

builder.Services.AddSerilog(x=> x.WriteTo.Console());

builder.Services.Configure<Neo4jSettings>(config.GetSection("neo4j"));
builder.Services.AddScoped<StandardsGraphRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<PositionStandartsParser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

using ARManagement.BaseRepository.Implement;
using ARManagement.BaseRepository.Interface;
using ARManagement.Helpers;
using Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region DBHelper
builder.Services.Configure<PostgreSqlDBConfig>(builder.Configuration.GetSection("DBConfig"));
builder.Services.AddTransient<IDatabaseHelper, DatabaseHelper>();
#endregion DBHelper

#region Repository 注入
builder.Services.AddTransient<IBaseRepository, BaseRepository>();
#endregion

#region Localizer多國語言
builder.Services.AddSingleton<ResponseCodeHelper>();
#endregion

#region CORS
// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("corsapp",
        builder =>
        {
            builder.WithOrigins("*")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
#endregion

#region JWT
builder.Services.AddSingleton<JwtHelper>();
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//app cors
app.UseCors("corsapp");

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();

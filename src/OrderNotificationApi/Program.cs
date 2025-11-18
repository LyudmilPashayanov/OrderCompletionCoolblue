var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/notify/{orderId:int}", (int orderId) =>
{
    var random = new Random();
    var shouldReturnOk = random.Next(2) == 0;
    if (shouldReturnOk)
    {
        return Results.Ok();
    }
    else
    {
        return Results.StatusCode(500);
    }
})
.WithName("Notify");

app.Run();
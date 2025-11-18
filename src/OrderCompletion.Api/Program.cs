using OrderCompletion.Api.Adapters.NotificationAdapter;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter;
using OrderCompletion.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.RegisterOrderCompletionAdapter(builder.Configuration);
builder.Services.RegisterNotificationAdapter();
builder.Services.RegisterDomainUseCases();
builder.Services.RegisterCors(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
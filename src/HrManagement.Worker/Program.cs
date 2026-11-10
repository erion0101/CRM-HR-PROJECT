using HrManagement.Application;
using HrManagement.Infrastructure;
using HrManagement.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DocumentProcessingWorker>();
var host = builder.Build();
await host.RunAsync();

using BookStore.Aspire.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var (postgres, db, rabbitmq, prometheus) = builder.AddBookStoreInfrastructure();
var webapi = builder.AddWebApi(db, postgres, rabbitmq, prometheus);
builder.AddWorker(db, postgres, rabbitmq, webapi, prometheus);

builder.Build().Run();

using BookStore.Aspire.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var (postgres, db, rabbitmq) = builder.AddBookStoreInfrastructure();
var webapi = builder.AddWebApi(db, postgres, rabbitmq);
builder.AddWorker(db, postgres, rabbitmq, webapi);

builder.Build().Run();

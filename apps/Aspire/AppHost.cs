using BookStore.Aspire.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var (postgres, db, rabbitmq, prometheus) = builder.AddBookStoreInfrastructure();
var fraudApi = builder.AddFraudApi(db, postgres, rabbitmq, prometheus);
builder.AddFraudWorker(db, postgres, rabbitmq, fraudApi, prometheus);
builder.AddBookStoreApi(db, postgres, rabbitmq, fraudApi, prometheus);

builder.Build().Run();

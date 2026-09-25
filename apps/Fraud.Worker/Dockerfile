FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY apps/Fraud.Worker/Fraud.Worker.csproj apps/Fraud.Worker/
COPY src/Application/Application.csproj src/Application/
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/

RUN dotnet restore apps/Fraud.Worker/Fraud.Worker.csproj

COPY apps/Fraud.Worker/ apps/Fraud.Worker/
COPY src/ src/

RUN dotnet publish apps/Fraud.Worker/Fraud.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fraud.Worker.dll"]

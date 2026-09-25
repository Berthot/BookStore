FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY apps/Fraud.Api/Fraud.Api.csproj apps/Fraud.Api/
COPY src/Application/Application.csproj src/Application/
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/

RUN dotnet restore apps/Fraud.Api/Fraud.Api.csproj

COPY apps/Fraud.Api/ apps/Fraud.Api/
COPY src/ src/

RUN dotnet publish apps/Fraud.Api/Fraud.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8081
ENTRYPOINT ["dotnet", "Fraud.Api.dll"]

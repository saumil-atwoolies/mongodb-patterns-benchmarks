# Stage 1: Build and test
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY MongoDbPatterns.slnx .
COPY nuget.config .
COPY src/MongoDbPatterns.Domain/MongoDbPatterns.Domain.csproj src/MongoDbPatterns.Domain/
COPY src/MongoDbPatterns.Infrastructure/MongoDbPatterns.Infrastructure.csproj src/MongoDbPatterns.Infrastructure/
COPY src/MongoDbPatterns.Benchmarks/MongoDbPatterns.Benchmarks.csproj src/MongoDbPatterns.Benchmarks/
COPY tests/MongoDbPatterns.Domain.Tests/MongoDbPatterns.Domain.Tests.csproj tests/MongoDbPatterns.Domain.Tests/
COPY tests/MongoDbPatterns.Infrastructure.Tests/MongoDbPatterns.Infrastructure.Tests.csproj tests/MongoDbPatterns.Infrastructure.Tests/

RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet build --no-restore -c Release

# Run tests (build fails if tests fail)
RUN dotnet test --no-build -c Release

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish src/MongoDbPatterns.Benchmarks/MongoDbPatterns.Benchmarks.csproj \
    --no-build -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MongoDbPatterns.Benchmarks.dll"]

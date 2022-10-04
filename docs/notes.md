# Build an dockerized container app in .NET

Ref: [https://blog.medhat.ca/2022/01/docker-compose-with-aspnet-60-sql-server.html](https://blog.medhat.ca/2022/01/docker-compose-with-aspnet-60-sql-server.html)

## Step 1: Create a razor app

```powershell
dotnet new razor -f net6.0 --auth individual -o AspMsSQL --use-local-db
cd AspMsSQL
dotnet run
```

## Step 2: Connect to a SQL Server instance

Comment out line 8 and 9 in `Program.cs`.

```csharp
// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<ApplicationDbContext>(options =>    options.UseSqlServer(connectionString));
```

and replace with

```csharp
var host = builder.Configuration["DBHOST"] ?? "127.0.0.1";
var port = builder.Configuration["DBPORT"] ?? "1444";
var user = builder.Configuration["DBUSER"] ?? "sa";
var pwd = builder.Configuration["DBPASSWORD"] ?? "SqlPassword!";
var db = builder.Configuration["DBNAME"] ?? "YellowDB";

var conStr = $"Server=tcp:{host},{port};Database={db};UID={user};PWD={pwd};";

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(conStr));
```

before `app.Run()` add the migration:

```csharp
using (var scope = app.Services.CreateScope()) {
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<ApplicationDbContext>();    
    context.Database.Migrate();
}
```

Run a SQL server instance:

```powershell
docker run --cap-add SYS_PTRACE -e ACCEPT_EULA=1 -e MSSQL_SA_PASSWORD=SqlPassword! -p 1444:1433 --name azsql -d mcr.microsoft.com/azure-sql-edge
```

Check it is running

```powershell
docker ps -a
```

And re-run the app

```powershell
dotnet run
```

Confirm it is working and you can register.

Connect to the SQL server in SSMS on `127.0.0.1,1444`

## Step 3: Dockerize the app

### Publish the app

First check whether the publish version works:

```powershell
dotnet publish -o dist
cd dist
dotnet AspMsSQL.dll
```

Let's stop the sql server

```powershell
docker rm -f azsql
cd ..
```

Open solution in Visual Studio and add `Docker support` or create a `Dockerfile`

```docker
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["AspMsSQL/AspMsSQL.csproj", "AspMsSQL/"]
RUN dotnet restore "AspMsSQL/AspMsSQL.csproj"
COPY . .
WORKDIR "/src/AspMsSQL"
RUN dotnet build "AspMsSQL.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AspMsSQL.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AspMsSQL.dll"]
```

Add `Docker Orchestration Support` and modify the docker-compose file

```docker
version: '3.8'

services:
  webapp:
    image: ${DOCKER_REGISTRY-}aspmssql
    build:
      context: .
      dockerfile: AspMsSQL/Dockerfile
    depends_on:
      - db
    ports:
      - "8888:80"
    restart: always
    environment:
      - DBHOST=db
      - DBPORT=1433
      - DBUSER=sa
      - DBPASSWORD=SqlPassword!
      - DBNAME=YellowDB
      - ASPNETCORE_ENVIRONMENT=Development

  db:
    image: mcr.microsoft.com/azure-sql-edge
    
    volumes:
      - sqlsystem:/var/opt/mssql/
      - sqldata:/var/opt/sqlserver/data
      - sqllog:/var/opt/sqlserver/log
      - sqlbackup:/var/opt/sqlserver/backup

    ports:
      - "1433:1433"
    restart: always
    
    environment:
      ACCEPT_EULA: Y
      MSSQL_SA_PASSWORD: SqlPassword!

volumes:
  sqlsystem:
  sqldata:
  sqllog:
  sqlbackup:
```

Run `docker-compose` project from Visual Studio.

Clean the solution

Run from commandline

```powershell
docker-compose build
docker-compose up -d
```

### Cleanup

```powershell
docker-compose down

docker rmi -f aspmssql
docker volume rm src_sqlbackup
docker volume rm src_sqldata
docker volume rm src_sqllog
docker volume rm src_sqlsystem
```

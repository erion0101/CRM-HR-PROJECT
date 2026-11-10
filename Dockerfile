FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props HrManagement.sln ./
COPY src/HrManagement.Domain/HrManagement.Domain.csproj src/HrManagement.Domain/
COPY src/HrManagement.Application/HrManagement.Application.csproj src/HrManagement.Application/
COPY src/HrManagement.Infrastructure/HrManagement.Infrastructure.csproj src/HrManagement.Infrastructure/
COPY src/HrManagement.Api/HrManagement.Api.csproj src/HrManagement.Api/
COPY src/HrManagement.Worker/HrManagement.Worker.csproj src/HrManagement.Worker/
RUN dotnet restore src/HrManagement.Api/HrManagement.Api.csproj && dotnet restore src/HrManagement.Worker/HrManagement.Worker.csproj
COPY src/ src/
RUN dotnet publish src/HrManagement.Api/HrManagement.Api.csproj -c Release --no-restore -o /out/api \
 && dotnet publish src/HrManagement.Worker/HrManagement.Worker.csproj -c Release --no-restore -o /out/worker

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "HrManagement.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
WORKDIR /app
COPY --from=build /out/worker .
USER $APP_UID
ENTRYPOINT ["dotnet", "HrManagement.Worker.dll"]

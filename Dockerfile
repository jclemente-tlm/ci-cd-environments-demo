FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json ci-cd-environments-demo.sln ./
COPY src/Cicd.Demo.Api/Cicd.Demo.Api.csproj src/Cicd.Demo.Api/
RUN dotnet restore src/Cicd.Demo.Api/Cicd.Demo.Api.csproj
COPY src/Cicd.Demo.Api/ src/Cicd.Demo.Api/
RUN dotnet publish src/Cicd.Demo.Api/Cicd.Demo.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cicd.Demo.Api.dll"]

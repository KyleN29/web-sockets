# --------- Build Stage ---------
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY WebApplication1.csproj ./
RUN dotnet restore WebApplication1.csproj

COPY . ./
RUN dotnet publish WebApplication1.csproj -c Release -o out

# --------- Runtime Stage ---------
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "WebApplication1.dll"]

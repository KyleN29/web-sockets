# --------- Build Stage ---------
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY MonkeyTyper.csproj ./
RUN dotnet restore MonkeyTyper.csproj

COPY . ./
RUN dotnet publish MonkeyTyper.csproj -c Release -o out

# --------- Runtime Stage ---------
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "MonkeyTyper.dll"]

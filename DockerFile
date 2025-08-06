# --------- Build Stage ---------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copy project file and restore
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the files
COPY . ./
RUN dotnet publish -c Release -o out

# --------- Runtime Stage ---------
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

# Copy from build stage
COPY --from=build /app/out .

# Let ASP.NET Core bind to the port Render assigns
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

# Open common ports
EXPOSE 80
EXPOSE 443

# Run the app
ENTRYPOINT ["dotnet", "WebApplication1.dll"]

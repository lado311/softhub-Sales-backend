FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SoftHub.API/SoftHub.API.csproj", "SoftHub.API/"]
RUN dotnet restore "SoftHub.API/SoftHub.API.csproj"
COPY . .
WORKDIR "/src/SoftHub.API"
RUN dotnet build "SoftHub.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SoftHub.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SoftHub.API.dll"]
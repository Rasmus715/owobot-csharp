FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["owobot-csharp.csproj", "./"]
RUN dotnet restore "owobot-csharp.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "owobot-csharp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "owobot-csharp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "owobot-csharp.dll"]

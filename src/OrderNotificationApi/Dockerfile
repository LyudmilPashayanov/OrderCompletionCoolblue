#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# If you use MX Mac, replace port 80 with port 8080 - you should also change this in the docker-compose.yaml file
# The docker-compose.yaml file can be found at <root>/docker-compose.yaml
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrderNotificationApi.csproj", "src/OrderNotificationApi/"]
RUN dotnet restore "src/OrderNotificationApi/OrderNotificationApi.csproj"

WORKDIR /src/src/OrderNotificationApi
COPY . .

RUN dotnet build . -c Release -o /app/build

FROM build AS publish

ARG RUNTIME
RUN dotnet publish "OrderNotificationApi.csproj" -c Release -o /app/publish --self-contained --runtime $RUNTIME

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderNotificationApi.dll"]
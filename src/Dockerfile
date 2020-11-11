FROM mcr.microsoft.com/dotnet/core/aspnet:5.0-buster-slim-arm32v7 AS base
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS publish
WORKDIR /src
COPY . .
RUN dotnet publish "IrrigationApi/IrrigationApi.csproj" --configuration Release --output /publish --runtime linux-arm --self-contained

FROM base AS final
WORKDIR /app
COPY --from=publish /publish .

ENTRYPOINT [ "dotnet", "IrrigationApi.dll"]
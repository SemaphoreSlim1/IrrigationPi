FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim-arm32v7 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS publish
WORKDIR /src
COPY src .
RUN dotnet publish "IrrigationApi/IrrigationApi.csproj" --configuration Release --output /publish --runtime linux-arm --self-contained


FROM base AS final
WORKDIR /app
COPY --from=publish /publish .

ENTRYPOINT [ "dotnet", "IrrigationApi.dll"]
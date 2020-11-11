ARG VERSION=1.0.0

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim-arm32v7 AS base
EXPOSE 80

#copy csproj files to their respective locations
#to create distinct layers
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS publish
WORKDIR /IrrigationApi
COPY IrrigationApi/IrrigationApi.csproj ./

#as we add additional projects to our main project,
#change the working directory as appropriate and copy in the csproj file

WORKDIR /IrrigationApi
RUN dotnet restore

RUN dotnet publish "IrrigationApi/IrrigationApi.csproj" \
            --configuration Release \
            --output /publish \
            --runtime linux-arm \
            --self-contained false \
            --no-restore \
            -p:PublishTrimmed=true \
            -p:TrimMode=Link \
            /p:Version=$VERSION

FROM base AS final
WORKDIR /app
COPY --from=publish /publish .

ENTRYPOINT [ "dotnet", "IrrigationApi.dll"]
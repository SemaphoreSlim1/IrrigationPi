ARG VERSION=1.0.0.0

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim-arm32v7 AS base
EXPOSE 80

#copy csproj files to their respective locations
#to create distinct layers
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS publish

#as we add additional projects to our main project,
#change the working directory as appropriate and copy in the csproj file
#then restore each project,
#then copy in the souce and publish it
#creating creating distinct layers in our image going from "least likely to change" to "most likely to chage"

#copy in the project and restore the dependencies
WORKDIR /source/HealthInvoker
COPY HealthInvoker/HealthInvoker.csproj .
RUN dotnet restore --runtime linux-arm

#copy in the source and publish
WORKDIR /source
COPY HealthInvoker HealthInvoker
WORKDIR /source/HealthInvoker
RUN dotnet publish HealthInvoker.csproj --configuration Release --runtime linux-arm --no-restore --output /publish/health -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=Link

#copy in the project and restore the dependencies
WORKDIR /source/IrrigationApi
COPY IrrigationApi/IrrigationApi.csproj .
RUN dotnet restore --runtime linux-arm

#copy in the source and publish
WORKDIR /source
COPY IrrigationApi IrrigationApi
WORKDIR /source/IrrigationApi
RUN dotnet publish IrrigationApi.csproj --configuration Release --runtime linux-arm --no-restore --output /publish/app -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=Link

FROM base AS final
WORKDIR /health
COPY --from=publish /publish/health .

WORKDIR /app
COPY --from=publish /publish/app .

WORKDIR /
HEALTHCHECK --interval=5s --timeout=10s --retries=3 CMD /health/HealthInvoker
ENTRYPOINT ["/app/IrrigationApi"]
ARG VERSION=1.0.0.0

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim-arm32v7 AS base
EXPOSE 80

#copy csproj files to their respective locations
#to create distinct layers
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS publish
WORKDIR /source/IrrigationApi
COPY IrrigationApi/IrrigationApi.csproj .

#as we add additional projects to our main project,
#change the working directory as appropriate and copy in the csproj file

#then restore each project, again creating distinct layers
WORKDIR /source/IrrigationApi
RUN dotnet restore --runtime linux-arm

#then copy in the source for each project
WORKDIR /source
COPY IrrigationApi IrrigationApi

#now publish the project
WORKDIR /source/IrrigationApi
RUN dotnet publish IrrigationApi.csproj --configuration Release --runtime linux-arm --no-restore --output /publish -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=Link

FROM base AS final
WORKDIR /app
COPY --from=publish /publish .

ENTRYPOINT ["./IrrigationApi"]
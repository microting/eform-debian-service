FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build-env
ARG GITVERSION
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY . ./
RUN dotnet publish MicrotingService/MicrotingService.csproj -o out /p:Version=$GITVERSION --runtime linux-x64 --configuration Release
RUN pwd

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
WORKDIR /app
COPY --from=build-env /app/out .

ENV DEBIAN_FRONTEND noninteractive
ENV Logging__Console__FormatterName=

ENTRYPOINT ["dotnet", "MicrotingService.dll"]

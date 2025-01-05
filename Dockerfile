FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build-env
ARG GITVERSION
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY . ./
RUN dotnet publish -o out /p:Version=$GITVERSION --runtime linux-x64 --configuration Release
RUN pwd

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim
WORKDIR /app
COPY --from=build-env /app/out .

ENV DEBIAN_FRONTEND noninteractive
ENV Logging__Console__FormatterName=

ENTRYPOINT ["dotnet", "MicrotingService.dll"]

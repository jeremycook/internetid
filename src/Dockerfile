# see https://github.com/dotnet/dotnet-docker/tree/main/samples/complexapp

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY InternetId.Server/*.csproj InternetId.Server/
COPY InternetId.OpenIddict/*.csproj InternetId.OpenIddict/
COPY InternetId.Users/*.csproj InternetId.Users/
COPY InternetId.Credentials/*.csproj InternetId.Credentials/
COPY InternetId.Common/*.csproj InternetId.Common/
COPY InternetId.EntityFrameworkCore/*.csproj InternetId.EntityFrameworkCore/
COPY InternetId.Npgsql/*.csproj InternetId.Npgsql/
RUN dotnet restore InternetId.Server/InternetId.Server.csproj

# copy and build app and libraries
COPY InternetId.Server/ InternetId.Server/
COPY InternetId.OpenIddict/ InternetId.OpenIddict/
COPY InternetId.Users/ InternetId.Users/
COPY InternetId.Credentials/ InternetId.Credentials/
COPY InternetId.Common/ InternetId.Common/
COPY InternetId.EntityFrameworkCore/ InternetId.EntityFrameworkCore/
COPY InternetId.Npgsql/ InternetId.Npgsql/
WORKDIR /source/InternetId.Server
RUN dotnet build -c release --no-restore

# test stage -- exposes optional entrypoint
# target entrypoint with: docker build --target test
FROM build AS test
WORKDIR /source/InternetId.Tests
COPY InternetId.Tests/ .
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

FROM build AS publish
RUN dotnet publish -c release --no-build -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "InternetId.Server.dll"]
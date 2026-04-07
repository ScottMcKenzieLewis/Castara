# Build/publish stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETRID=linux-x64

WORKDIR /src

# Native AOT prerequisites for Ubuntu
RUN apt-get update \
    && apt-get install -y --no-install-recommends clang zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

COPY ["src/Castara.Web.Api/Castara.Web.Api.csproj", "src/Castara.Web.Api/"]
RUN dotnet restore "src/Castara.Web.Api/Castara.Web.Api.csproj" -r ${TARGETRID}

COPY . .
WORKDIR /src/src/Castara.Web.Api

RUN dotnet publish "Castara.Web.Api.csproj" \
    -c ${BUILD_CONFIGURATION} \
    -r ${TARGETRID} \
    -o /app/publish \
    --self-contained true

# Final runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy AS final
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["./Castara.Web.Api"]
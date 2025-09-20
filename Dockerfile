FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY . .

# Restore and publish. Ensure the project has access to dotnet-ef by installing the global tool during build
RUN dotnet tool install --global dotnet-ef --version 9.0.0 || true
ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/out ./
COPY --from=build /app/docker-entrypoint.sh ./
RUN chmod +x ./docker-entrypoint.sh


ENTRYPOINT ["/bin/sh", "./docker-entrypoint.sh"]
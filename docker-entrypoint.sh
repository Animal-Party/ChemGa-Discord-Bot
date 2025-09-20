#!/bin/sh
# Entry point script to run EF Core migrations before starting the app

set -e

MAX_RETRIES=${MAX_RETRIES:-5}
SLEEP_SECONDS=${SLEEP_SECONDS:-5}

echo "Starting entrypoint: will attempt EF migrations up to $MAX_RETRIES times"

i=0
until [ $i -ge $MAX_RETRIES ]
do
  i=$((i+1))
  echo "Attempt $i/$MAX_RETRIES: running 'dotnet ef database update'"
  if dotnet ef database update --no-build; then
    echo "Migrations applied successfully"
    break
  else
    echo "Migrations failed on attempt $i. Retrying in $SLEEP_SECONDS seconds..."
    sleep $SLEEP_SECONDS
  fi
done

if [ $i -ge $MAX_RETRIES ]; then
  echo "Failed to apply migrations after $MAX_RETRIES attempts. Exiting."
  exit 1
fi

echo "Starting application: dotnet ChemGa.dll"
exec dotnet ChemGa.dll

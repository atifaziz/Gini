#!/usr/bin/env bash
set -e
dotnet --info
cd "$(dirname "$0")"
dotnet restore
for c in Debug Release; do
    dotnet build --no-restore -c $c
done

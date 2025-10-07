#!/bin/bash

# Quick start script for Marain Tenancy API
# This script navigates to the API project and runs it

set -e

echo "Starting Marain Tenancy API..."

cd Solutions/Marain.Tenancy.Api
dotnet run

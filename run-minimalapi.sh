#!/bin/bash

# Run script for Marain.Tenancy.MinimalApi
# This script starts the MinimalApi service accessible from outside the dev container

echo "Starting Marain.Tenancy.MinimalApi..."
echo "API will be accessible at:"
echo "  HTTP:  http://localhost:5138"
echo "  HTTPS: https://localhost:7124"
echo "  Swagger UI: http://localhost:5138/swagger"
echo ""

# Navigate to the MinimalApi project directory
cd "$(dirname "$0")/Solutions/Marain.Tenancy.MinimalApi"

# Run the application
exec dotnet run --launch-profile http
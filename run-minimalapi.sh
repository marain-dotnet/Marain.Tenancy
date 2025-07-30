#!/bin/bash

# Run script for Marain.Tenancy.MinimalApi
# This script starts the MinimalApi service accessible from outside the dev container

echo "Starting Marain.Tenancy.MinimalApi..."

# Check if port 5138 is already in use
if netstat -tlnp 2>/dev/null | grep -q ":5138 " || ss -tlnp | grep -q ":5138 "; then
    echo "❌ Error: Port 5138 is already in use!"
    echo ""
    echo "To fix this, run:"
    echo "  ./manage-api.sh stop    # Stop any running instances"
    echo "  ./manage-api.sh start   # Start fresh"
    echo ""
    echo "Or use the management script directly:"
    echo "  ./manage-api.sh restart"
    exit 1
fi

echo "API will be accessible at:"
echo "  HTTP:         http://localhost:5138"
echo "  HTTPS:        https://localhost:7124"
echo "  Swagger JSON: http://localhost:5138/swagger"
echo "  Swagger UI:   http://localhost:5138/swagger-ui"
echo "  Health:       http://localhost:5138/health"
echo ""

# Navigate to the MinimalApi project directory
cd "$(dirname "$0")/Solutions/Marain.Tenancy.MinimalApi"

# Run the application
exec dotnet run --launch-profile http

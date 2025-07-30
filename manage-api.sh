#!/bin/bash

# Management script for Marain.Tenancy.MinimalApi

API_PORT=5138
API_DIR="Solutions/Marain.Tenancy.MinimalApi"

# Function to check if API is running
check_api() {
    if netstat -tlnp 2>/dev/null | grep -q ":$API_PORT " || ss -tlnp | grep -q ":$API_PORT "; then
        echo "✅ API is running on port $API_PORT"
        return 0
    else
        echo "❌ API is not running on port $API_PORT"
        return 1
    fi
}

# Function to stop API
stop_api() {
    echo "Stopping MinimalApi..."
    
    # Find and kill dotnet processes running the MinimalApi
    pids=$(ps aux | grep "Marain.Tenancy.MinimalApi" | grep -v grep | awk '{print $2}')
    if [ -n "$pids" ]; then
        echo "Killing processes: $pids"
        echo $pids | xargs kill
        sleep 2
    fi
    
    # Double-check by killing anything using port 5138
    port_pid=$(netstat -tlnp 2>/dev/null | grep ":$API_PORT " | awk '{print $7}' | cut -d'/' -f1)
    if [ -n "$port_pid" ] && [ "$port_pid" != "-" ]; then
        echo "Killing process using port $API_PORT: $port_pid"
        kill $port_pid 2>/dev/null
    fi
    
    sleep 1
    
    if check_api > /dev/null 2>&1; then
        echo "❌ Failed to stop API"
        return 1
    else
        echo "✅ API stopped successfully"
        return 0
    fi
}

# Function to start API
start_api() {
    echo "Starting MinimalApi..."
    
    if check_api > /dev/null 2>&1; then
        echo "⚠️  API is already running. Use 'stop' first or 'restart'"
        return 1
    fi
    
    echo "API will be accessible at:"
    echo "  HTTP:         http://localhost:5138"
    echo "  HTTPS:        https://localhost:7124"
    echo "  Swagger JSON: http://localhost:5138/swagger"
    echo "  Swagger UI:   http://localhost:5138/swagger-ui"
    echo "  Health:       http://localhost:5138/health"
    echo ""
    
    cd "$API_DIR" || { echo "❌ Could not navigate to $API_DIR"; exit 1; }
    exec dotnet run --launch-profile http
}

# Function to restart API
restart_api() {
    echo "Restarting MinimalApi..."
    stop_api
    sleep 2
    start_api
}

# Main script logic
case "$1" in
    "start")
        start_api
        ;;
    "stop")
        stop_api
        ;;
    "restart")
        restart_api
        ;;
    "status")
        check_api
        ;;
    "test")
        echo "Testing API endpoints..."
        if check_api > /dev/null 2>&1; then
            echo "🔍 Testing health endpoint:"
            curl -s http://localhost:5138/health && echo ""
            echo "🔍 Testing swagger endpoint:"
            curl -s http://localhost:5138/swagger | head -3
            echo "..."
        else
            echo "❌ API is not running. Start it first with: $0 start"
        fi
        ;;
    *)
        echo "Usage: $0 {start|stop|restart|status|test}"
        echo ""
        echo "Commands:"
        echo "  start   - Start the MinimalApi"
        echo "  stop    - Stop the MinimalApi"
        echo "  restart - Restart the MinimalApi"
        echo "  status  - Check if API is running"
        echo "  test    - Test API endpoints"
        echo ""
        echo "Examples:"
        echo "  $0 start     # Start the API"
        echo "  $0 status    # Check if running"
        echo "  $0 test      # Test endpoints"
        echo "  $0 stop      # Stop the API"
        exit 1
        ;;
esac
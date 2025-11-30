#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Error handling - don't exit on error immediately, handle manually
set -o pipefail  # Exit on pipe failure

echo -e "${GREEN}MMGC Docker Setup Script${NC}"
echo "================================"

# Check if .env file exists
if [ ! -f .env ]; then
    echo -e "${YELLOW}Creating .env file from .env.example...${NC}"
    if [ -f .env.example ]; then
        cp .env.example .env
        echo -e "${GREEN}.env file created. Please edit it with your credentials if needed.${NC}"
    else
        echo -e "${RED}Error: .env.example file not found!${NC}"
        exit 1
    fi
fi

# Load environment variables safely
if [ -f .env ]; then
    set -a  # Automatically export all variables
    source .env
    set +a
fi

# Validate required environment variables
if [ -z "$MSSQL_SA_PASSWORD" ]; then
    echo -e "${RED}Error: MSSQL_SA_PASSWORD is not set in .env file${NC}"
    exit 1
fi

if [ -z "$MSSQL_DATABASE" ]; then
    echo -e "${RED}Error: MSSQL_DATABASE is not set in .env file${NC}"
    exit 1
fi

# Set default ports if not specified
MSSQL_PORT=${MSSQL_PORT:-5001}
SQLPAD_PORT=${SQLPAD_PORT:-5002}
WEBAPP_PORT=${WEBAPP_PORT:-5003}

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Error: Docker is not running. Please start Docker and try again.${NC}"
    exit 1
fi

# Check if Docker Compose is available
DOCKER_COMPOSE_CMD="docker-compose"
if ! command -v docker-compose &> /dev/null; then
    if docker compose version &> /dev/null; then
        DOCKER_COMPOSE_CMD="docker compose"
    else
        echo -e "${RED}Error: Docker Compose is not installed.${NC}"
        exit 1
    fi
fi

# Function to check if port is in use
check_port() {
    local port=$1
    local service=$2
    local port_in_use=false
    
    # Try different methods to check if port is in use
    if command -v lsof >/dev/null 2>&1; then
        if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
            port_in_use=true
        fi
    elif command -v netstat >/dev/null 2>&1; then
        if netstat -tuln 2>/dev/null | grep -q ":$port "; then
            port_in_use=true
        fi
    elif command -v ss >/dev/null 2>&1; then
        if ss -tuln 2>/dev/null | grep -q ":$port "; then
            port_in_use=true
        fi
    fi
    
    if [ "$port_in_use" = true ]; then
        echo -e "${RED}Error: Port $port is already in use. Please change ${service}_PORT in .env file${NC}"
        exit 1
    fi
}

# Check for port conflicts
echo -e "${BLUE}Checking for port conflicts...${NC}"
check_port "$MSSQL_PORT" "MSSQL"
check_port "$SQLPAD_PORT" "SQLPAD"
check_port "$WEBAPP_PORT" "WEBAPP"
echo -e "${GREEN}All ports are available${NC}"

# Check for existing containers and remove them
echo -e "${BLUE}Checking for existing containers...${NC}"
CONTAINERS=("mmgc-mssql" "mmgc-sqlpad" "mmgc-webapp")
for container in "${CONTAINERS[@]}"; do
    if docker ps -a --format '{{.Names}}' | grep -q "^${container}$"; then
        echo -e "${YELLOW}Found existing container: $container${NC}"
        if docker ps --format '{{.Names}}' | grep -q "^${container}$"; then
            echo -e "${YELLOW}Stopping container: $container${NC}"
            docker stop "$container" || true
        fi
        echo -e "${YELLOW}Removing container: $container${NC}"
        docker rm "$container" || true
    fi
done

# Stop and remove containers using docker-compose if they exist
echo -e "${BLUE}Cleaning up docker-compose resources...${NC}"
$DOCKER_COMPOSE_CMD down -v 2>/dev/null || true

# Build Docker images
echo -e "${GREEN}Building Docker images...${NC}"
if ! $DOCKER_COMPOSE_CMD build --no-cache; then
    echo -e "${RED}Error: Failed to build Docker images${NC}"
    exit 1
fi

# Start containers
echo -e "${GREEN}Starting containers...${NC}"
if ! $DOCKER_COMPOSE_CMD up -d; then
    echo -e "${RED}Error: Failed to start containers${NC}"
    exit 1
fi

# Wait for MSSQL to be ready
echo -e "${YELLOW}Waiting for MSSQL Server to be ready...${NC}"
MAX_ATTEMPTS=60
ATTEMPT=0
MSSQL_READY=false

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    if docker exec mmgc-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" -b &> /dev/null 2>&1; then
        echo -e "${GREEN}MSSQL Server is ready!${NC}"
        MSSQL_READY=true
        break
    fi
    ATTEMPT=$((ATTEMPT + 1))
    if [ $((ATTEMPT % 5)) -eq 0 ]; then
        echo -e "${YELLOW}Waiting for MSSQL Server... (Attempt $ATTEMPT/$MAX_ATTEMPTS)${NC}"
    fi
    sleep 2
done

if [ "$MSSQL_READY" = false ]; then
    echo -e "${RED}Error: MSSQL Server did not become ready in time.${NC}"
    echo -e "${YELLOW}Check logs with: $DOCKER_COMPOSE_CMD logs mssql${NC}"
    exit 1
fi

# Create database if it doesn't exist
echo -e "${GREEN}Creating database if it doesn't exist...${NC}"
if ! docker exec mmgc-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '${MSSQL_DATABASE}') CREATE DATABASE [${MSSQL_DATABASE}]" -b 2>&1; then
    echo -e "${YELLOW}Warning: Could not create database (it may already exist)${NC}"
fi

# Wait for webapp container to be ready
echo -e "${YELLOW}Waiting for webapp container to be ready...${NC}"
MAX_ATTEMPTS=30
ATTEMPT=0
WEBAPP_READY=false

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    if docker ps --format '{{.Names}}' | grep -q "^mmgc-webapp$"; then
        if docker exec mmgc-webapp dotnet --version &> /dev/null 2>&1; then
            echo -e "${GREEN}Webapp container is ready!${NC}"
            WEBAPP_READY=true
            break
        fi
    fi
    ATTEMPT=$((ATTEMPT + 1))
    sleep 2
done

if [ "$WEBAPP_READY" = false ]; then
    echo -e "${YELLOW}Warning: Webapp container may not be fully ready yet${NC}"
fi

# Run migrations using migrate.sh script
echo -e "${GREEN}Running EF Core migrations...${NC}"
if [ -f "./migrate.sh" ]; then
    chmod +x ./migrate.sh
    if ./migrate.sh update; then
        echo -e "${GREEN}Migrations completed successfully!${NC}"
    else
        echo -e "${YELLOW}Note: Migrations may need to be created first.${NC}"
        echo -e "${YELLOW}Run: ./migrate.sh add InitialCreate${NC}"
    fi
else
    echo -e "${YELLOW}Warning: migrate.sh not found, attempting direct migration...${NC}"
    if docker exec mmgc-webapp dotnet ef database update --project MMGC.csproj 2>&1; then
        echo -e "${GREEN}Migrations completed successfully!${NC}"
    else
        echo -e "${YELLOW}Note: If migrations fail, you may need to create an initial migration first.${NC}"
        echo -e "${YELLOW}Run: docker exec -it mmgc-webapp dotnet ef migrations add InitialCreate --project MMGC.csproj${NC}"
    fi
fi

# Wait a moment for seeding to complete
echo -e "${YELLOW}Waiting for database seeding to complete...${NC}"
sleep 5

# Restart webapp container to ensure seeding runs
echo -e "${GREEN}Restarting webapp container to apply seeding...${NC}"
$DOCKER_COMPOSE_CMD restart webapp

echo ""
echo -e "${GREEN}================================${NC}"
echo -e "${GREEN}Setup completed successfully!${NC}"
echo -e "${GREEN}================================${NC}"
echo ""
echo -e "Services are running:"
echo -e "  - Web App:     http://localhost:${WEBAPP_PORT}"
echo -e "  - SQLPad:      http://localhost:${SQLPAD_PORT}"
echo -e "  - MSSQL:       localhost:${MSSQL_PORT}"
echo ""
echo -e "${BLUE}Default Admin Credentials:${NC}"
echo -e "  - Email:    admin@mmgc.com"
echo -e "  - Password: Admin@123"
echo ""
echo -e "${BLUE}Created Roles:${NC}"
echo -e "  - Admin (Full system access)"
echo -e "  - Doctor (Doctor management)"
echo -e "  - Nurse (Nurse management)"
echo -e "  - LabStaff (Laboratory management)"
echo -e "  - Patient (Patient access)"
echo ""
echo -e "Useful commands:"
echo -e "  - View logs:   $DOCKER_COMPOSE_CMD logs -f"
echo -e "  - Stop:        $DOCKER_COMPOSE_CMD down"
echo -e "  - Restart:     $DOCKER_COMPOSE_CMD restart"
echo -e "  - Migrations:  ./migrate.sh update"
echo ""

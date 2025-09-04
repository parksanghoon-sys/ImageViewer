@echo off
echo "Starting microservices with Docker Compose..."

cd /d "D:\MyStudy\05.WebProject\07.ImageViewer"

echo "Stopping existing containers..."
docker compose down

echo "Building and starting all services..."
docker compose up -d --build

echo "Checking service status..."
docker compose ps

pause
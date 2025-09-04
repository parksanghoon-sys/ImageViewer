#!/bin/bash

echo "🚀 Starting ImageViewer services..."

# Use development environment by default
docker-compose --env-file .env up --build -d

echo "✅ Services started successfully!"
echo ""
echo "📊 Access URLs:"
echo "- API Gateway: http://localhost:5000"
echo "- RabbitMQ Management: http://localhost:15672 (guest/guest)"
echo "- PostgreSQL: localhost:5432"
echo ""
echo "🔧 Useful commands:"
echo "- View logs: docker-compose logs -f [service_name]"
echo "- Stop services: docker-compose down"
echo "- Stop and remove volumes: docker-compose down -v"
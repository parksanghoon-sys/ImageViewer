#!/bin/bash

echo "🏭 Starting ImageViewer services (Production mode)..."

# Use production environment
docker-compose --env-file .env.prod up --build -d

echo "✅ Production services started successfully!"
echo ""
echo "📊 Access URLs:"
echo "- API Gateway: http://localhost:5000"
echo "- RabbitMQ Management: http://localhost:15672"
echo "- PostgreSQL: localhost:5432"
echo ""
echo "🔧 Useful commands:"
echo "- View logs: docker-compose logs -f [service_name]"
echo "- Stop services: docker-compose down"
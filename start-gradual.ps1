# Quick Start Script for Gradual Migration
# This script helps you test the microservices setup

Write-Host "🚀 Starting Gradual Migration Setup..." -ForegroundColor Green
Write-Host ""

# Check if Docker is running
Write-Host "📋 Checking Docker..." -ForegroundColor Yellow
$dockerRunning = docker info 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Docker is running" -ForegroundColor Green
Write-Host ""

# Stop any existing containers
Write-Host "🛑 Stopping existing containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.gradual.yml down 2>&1 | Out-Null
Write-Host "✅ Cleaned up existing containers" -ForegroundColor Green
Write-Host ""

# Build and start services
Write-Host "🏗️  Building and starting services (this may take a few minutes)..." -ForegroundColor Yellow
docker-compose -f docker-compose.gradual.yml up --build -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start services. Check the error messages above." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Services started successfully!" -ForegroundColor Green
Write-Host ""

# Wait for services to be ready
Write-Host "⏳ Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Check service health
Write-Host ""
Write-Host "🏥 Checking service health..." -ForegroundColor Yellow
Write-Host ""

Write-Host "  SQL Server: " -NoNewline -ForegroundColor Cyan
$sqlHealth = docker inspect --format='{{.State.Health.Status}}' assignment_grading_db 2>&1
if ($sqlHealth -eq "healthy") {
    Write-Host "✅ Healthy" -ForegroundColor Green
} else {
    Write-Host "⚠️  Starting... (Status: $sqlHealth)" -ForegroundColor Yellow
}

Write-Host "  Monolith API: " -NoNewline -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/swagger/index.html" -TimeoutSec 2 -UseBasicParsing 2>$null
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Running" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Starting..." -ForegroundColor Yellow
}

Write-Host "  IdentityService: " -NoNewline -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5001/api/auth/health" -TimeoutSec 2 -UseBasicParsing 2>$null
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Running" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Starting..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🎉 Setup Complete!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""
Write-Host "📍 Service URLs:" -ForegroundColor Cyan
Write-Host "   Monolith API:      http://localhost:5000/swagger" -ForegroundColor White
Write-Host "   IdentityService:   http://localhost:5001/swagger" -ForegroundColor White
Write-Host "   SQL Server:        localhost:1433" -ForegroundColor White
Write-Host ""
Write-Host "🧪 Quick Test Commands:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   # Health check" -ForegroundColor Gray
Write-Host "   curl http://localhost:5001/api/auth/health" -ForegroundColor White
Write-Host ""
Write-Host "   # Register user" -ForegroundColor Gray
Write-Host '   curl -X POST http://localhost:5001/api/auth/register -H "Content-Type: application/json" -d "{\"username\":\"test\",\"email\":\"test@test.com\",\"password\":\"Test@123\",\"role\":\"Examiner\"}"' -ForegroundColor White
Write-Host ""
Write-Host "   # Login" -ForegroundColor Gray
Write-Host '   curl -X POST http://localhost:5001/api/auth/login -H "Content-Type: application/json" -d "{\"username\":\"test\",\"password\":\"Test@123\"}"' -ForegroundColor White
Write-Host ""
Write-Host "📊 View Logs:" -ForegroundColor Cyan
Write-Host "   docker-compose -f docker-compose.gradual.yml logs -f" -ForegroundColor White
Write-Host ""
Write-Host "🛑 Stop Services:" -ForegroundColor Cyan
Write-Host "   docker-compose -f docker-compose.gradual.yml down" -ForegroundColor White
Write-Host ""
Write-Host "📖 Read GRADUAL_MIGRATION.md for detailed guide" -ForegroundColor Yellow
Write-Host ""

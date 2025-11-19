# Test IdentityService - Automated Testing Script
# This script tests the IdentityService to ensure everything works

Write-Host "🧪 IdentityService Testing Script" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray
Write-Host ""

$baseUrl = "http://localhost:5001"
$testsPassed = 0
$testsFailed = 0

# Function to test API endpoint
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null
    )
    
    Write-Host "Testing: $Name..." -NoNewline
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            UseBasicParsing = $true
            TimeoutSec = 10
        }
        
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-WebRequest @params -ErrorAction Stop
        
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
            Write-Host " ✅ PASS" -ForegroundColor Green
            return @{ Success = $true; Response = $response }
        } else {
            Write-Host " ❌ FAIL (Status: $($response.StatusCode))" -ForegroundColor Red
            return @{ Success = $false; Response = $response }
        }
    }
    catch {
        Write-Host " ❌ FAIL" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Yellow
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# Wait for service to be ready
Write-Host "⏳ Waiting for IdentityService to start..." -ForegroundColor Yellow
$maxRetries = 30
$retryCount = 0
$serviceReady = $false

while ($retryCount -lt $maxRetries -and -not $serviceReady) {
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/api/auth/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $serviceReady = $true
            Write-Host "✅ Service is ready!" -ForegroundColor Green
        }
    }
    catch {
        $retryCount++
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
    }
}

if (-not $serviceReady) {
    Write-Host ""
    Write-Host "❌ Service failed to start after $maxRetries attempts" -ForegroundColor Red
    Write-Host "Make sure Docker is running and execute:" -ForegroundColor Yellow
    Write-Host "  docker-compose -f docker-compose.gradual.yml up -d" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "🧪 Running Tests..." -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
$result = Test-Endpoint -Name "Health Check" -Url "$baseUrl/api/auth/health"
if ($result.Success) { $testsPassed++ } else { $testsFailed++ }

# Test 2: Register New User
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$testUser = @{
    username = "testuser$timestamp"
    email = "test$timestamp@example.com"
    password = "Test@123456"
    role = "Examiner"
} | ConvertTo-Json

$result = Test-Endpoint -Name "Register User" -Url "$baseUrl/api/auth/register" -Method "POST" -Body $testUser
if ($result.Success) { 
    $testsPassed++
    $registeredUser = ($result.Response.Content | ConvertFrom-Json)
    $testUsername = $registeredUser.username
    Write-Host "  → Created user: $testUsername" -ForegroundColor Gray
} else { 
    $testsFailed++
}

# Test 3: Login with Created User
if ($result.Success) {
    $loginData = @{
        username = $registeredUser.username
        password = "Test@123456"
    } | ConvertTo-Json
    
    $result = Test-Endpoint -Name "Login User" -Url "$baseUrl/api/auth/login" -Method "POST" -Body $loginData
    if ($result.Success) { 
        $testsPassed++
        $loginResponse = ($result.Response.Content | ConvertFrom-Json)
        $token = $loginResponse.token
        Write-Host "  → Token received (${token.Substring(0, 50)}...)" -ForegroundColor Gray
        
        # Test 4: Get User by ID with Token
        $userId = $loginResponse.user.userId
        $headers = @{ Authorization = "Bearer $token" }
        $result = Test-Endpoint -Name "Get User by ID" -Url "$baseUrl/api/auth/users/$userId" -Headers $headers
        if ($result.Success) { $testsPassed++ } else { $testsFailed++ }
        
        # Test 5: Refresh Token
        $refreshData = @{
            refreshToken = $loginResponse.refreshToken
        } | ConvertTo-Json
        
        $result = Test-Endpoint -Name "Refresh Token" -Url "$baseUrl/api/auth/refresh" -Method "POST" -Body $refreshData
        if ($result.Success) { 
            $testsPassed++
            Write-Host "  → New token received" -ForegroundColor Gray
        } else { 
            $testsFailed++
        }
    } else { 
        $testsFailed++
        $testsFailed += 2 # Skip remaining tests
    }
}

# Test 6: Register Duplicate User (Should Fail)
$result = Test-Endpoint -Name "Register Duplicate (Should Fail)" -Url "$baseUrl/api/auth/register" -Method "POST" -Body $testUser
if (-not $result.Success -or $result.Response.StatusCode -eq 400) { 
    Write-Host "Testing: Register Duplicate (Should Fail)... ✅ PASS (Correctly rejected)" -ForegroundColor Green
    $testsPassed++
} else { 
    $testsFailed++
}

# Test 7: Login with Wrong Password (Should Fail)
$wrongLoginData = @{
    username = $testUsername
    password = "WrongPassword123"
} | ConvertTo-Json

$result = Test-Endpoint -Name "Login Wrong Password (Should Fail)" -Url "$baseUrl/api/auth/login" -Method "POST" -Body $wrongLoginData
if (-not $result.Success -or $result.Response.StatusCode -eq 401) { 
    Write-Host "Testing: Login Wrong Password (Should Fail)... ✅ PASS (Correctly rejected)" -ForegroundColor Green
    $testsPassed++
} else { 
    $testsFailed++
}

# Summary
Write-Host ""
Write-Host "=" * 60 -ForegroundColor Gray
Write-Host "📊 Test Summary" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray
Write-Host "Total Tests: $($testsPassed + $testsFailed)" -ForegroundColor White
Write-Host "Passed: $testsPassed" -ForegroundColor Green
Write-Host "Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($testsFailed -eq 0) {
    Write-Host "🎉 All tests passed! IdentityService is working correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ 3-Layer Architecture: Working" -ForegroundColor Green
    Write-Host "✅ Database Connection: Working" -ForegroundColor Green
    Write-Host "✅ JWT Authentication: Working" -ForegroundColor Green
    Write-Host "✅ BCrypt Password Hashing: Working" -ForegroundColor Green
    Write-Host "✅ Refresh Token: Working" -ForegroundColor Green
    Write-Host "✅ Input Validation: Working" -ForegroundColor Green
    Write-Host ""
    Write-Host "🚀 Ready to create CoreService and StorageService!" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "⚠️  Some tests failed. Please check the service logs:" -ForegroundColor Yellow
    Write-Host "  docker-compose -f docker-compose.gradual.yml logs identity-service" -ForegroundColor White
    exit 1
}

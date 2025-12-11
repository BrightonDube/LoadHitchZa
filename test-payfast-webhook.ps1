# PayFast Webhook Testing Script with ngrok
# This script helps test PayFast webhook integration locally

Write-Host "=== PayFast Webhook Testing Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if ngrok is installed
$ngrokInstalled = Get-Command ngrok -ErrorAction SilentlyContinue
if (-not $ngrokInstalled) {
    Write-Host "❌ ngrok is not installed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Install ngrok:" -ForegroundColor Yellow
    Write-Host "1. Visit: https://ngrok.com/download" -ForegroundColor Yellow
    Write-Host "2. Download Windows version" -ForegroundColor Yellow
    Write-Host "3. Extract and add to PATH, or:" -ForegroundColor Yellow
    Write-Host "   choco install ngrok" -ForegroundColor Yellow
    Write-Host "   or" -ForegroundColor Yellow
    Write-Host "   winget install ngrok" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ ngrok is installed" -ForegroundColor Green
Write-Host ""

# Step 1: Start the application
Write-Host "Step 1: Start your application" -ForegroundColor Cyan
Write-Host "Run this in a separate terminal:" -ForegroundColor Yellow
Write-Host "  dotnet run --project .\cse325t12Project.csproj" -ForegroundColor White
Write-Host ""
Write-Host "Press Enter when your application is running on https://localhost:5001..." -ForegroundColor Yellow
$null = Read-Host

# Step 2: Start ngrok tunnel
Write-Host "Step 2: Starting ngrok tunnel..." -ForegroundColor Cyan
Write-Host ""
Write-Host "Run this in a separate terminal:" -ForegroundColor Yellow
Write-Host "  ngrok http 5001" -ForegroundColor White
Write-Host ""
Write-Host "After ngrok starts, you'll see a Forwarding URL like:" -ForegroundColor Yellow
Write-Host "  https://xxxx-xx-xx-xxx-xxx.ngrok-free.app" -ForegroundColor White
Write-Host ""
Write-Host "Enter your ngrok public URL (e.g., https://xxxx.ngrok-free.app):" -ForegroundColor Yellow
$ngrokUrl = Read-Host

if ([string]::IsNullOrWhiteSpace($ngrokUrl)) {
    Write-Host "❌ No URL provided" -ForegroundColor Red
    exit 1
}

# Remove trailing slash
$ngrokUrl = $ngrokUrl.TrimEnd('/')

Write-Host ""
Write-Host "✓ Using ngrok URL: $ngrokUrl" -ForegroundColor Green
Write-Host ""

# Step 3: PayFast Configuration
Write-Host "Step 3: Configure PayFast Sandbox" -ForegroundColor Cyan
Write-Host ""
Write-Host "PayFast Webhook URLs to configure:" -ForegroundColor Yellow
Write-Host "  Notify URL (ITN): $ngrokUrl/api/payfast/notify" -ForegroundColor White
Write-Host "  Return URL:       $ngrokUrl/api/payfast/return" -ForegroundColor White
Write-Host "  Cancel URL:       $ngrokUrl/api/payfast/cancel" -ForegroundColor White
Write-Host ""

# Step 4: Test Payment Form
Write-Host "Step 4: Test Payment Scenarios" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Cards (PayFast Sandbox):" -ForegroundColor Yellow
Write-Host "  Successful: 4000000000000002" -ForegroundColor Green
Write-Host "  Declined:   5200000000000015" -ForegroundColor Red
Write-Host ""
Write-Host "Card Details:" -ForegroundColor Yellow
Write-Host "  CVV: Any 3 digits (e.g., 123)" -ForegroundColor White
Write-Host "  Expiry: Any future date (e.g., 12/25)" -ForegroundColor White
Write-Host ""

# Step 5: Create test payment
Write-Host "Step 5: Create Test Payment in Database" -ForegroundColor Cyan
Write-Host ""
Write-Host "You need to create a test payment record. Run this SQL:" -ForegroundColor Yellow
Write-Host ""
Write-Host @"
-- Create test payment (run in your database)
INSERT INTO ""Payments"" (""TransactionId"", ""CustomerId"", ""DriverId"", ""LoadId"", ""Amount"", ""Status"", ""CreatedAt"")
VALUES (
    'TEST-' || CAST(EXTRACT(EPOCH FROM NOW()) AS VARCHAR),
    (SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" LIKE '%customer%' LIMIT 1),
    (SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" LIKE '%driver%' LIMIT 1),
    (SELECT ""Id"" FROM ""Loads"" ORDER BY ""CreatedAt"" DESC LIMIT 1),
    250.00,
    'Pending',
    NOW()
);

-- Get the payment ID
SELECT ""Id"", ""TransactionId"", ""Amount"", ""Status"" 
FROM ""Payments"" 
ORDER BY ""CreatedAt"" DESC 
LIMIT 1;
"@ -ForegroundColor White
Write-Host ""

# Step 6: Generate PayFast form
Write-Host "Step 6: Test Payment Flow" -ForegroundColor Cyan
Write-Host ""
Write-Host "In your application:" -ForegroundColor Yellow
Write-Host "1. Navigate to create load page" -ForegroundColor White
Write-Host "2. Complete load details (pickup, delivery, weight)" -ForegroundColor White
Write-Host "3. View calculated price" -ForegroundColor White
Write-Host "4. Click 'Pay Now' button" -ForegroundColor White
Write-Host "5. Verify redirect to PayFast" -ForegroundColor White
Write-Host "6. Complete payment with test card" -ForegroundColor White
Write-Host "7. Monitor webhook in application logs" -ForegroundColor White
Write-Host ""

# Step 7: Monitor logs
Write-Host "Step 7: Monitor Application Logs" -ForegroundColor Cyan
Write-Host ""
Write-Host "Watch for these log entries:" -ForegroundColor Yellow
Write-Host "  • PayFast notification received" -ForegroundColor White
Write-Host "  • Signature validation result" -ForegroundColor White
Write-Host "  • Payment status update" -ForegroundColor White
Write-Host "  • Database transaction completed" -ForegroundColor White
Write-Host ""

# Step 8: Verify database
Write-Host "Step 8: Verify Database Updates" -ForegroundColor Cyan
Write-Host ""
Write-Host "Check payment status updated:" -ForegroundColor Yellow
Write-Host @"
SELECT 
    p.""Id"", 
    p.""TransactionId"", 
    p.""Amount"", 
    p.""Status"", 
    p.""PayFastPaymentId"",
    p.""UpdatedAt""
FROM ""Payments"" p
ORDER BY p.""UpdatedAt"" DESC
LIMIT 5;
"@ -ForegroundColor White
Write-Host ""

# Summary
Write-Host "=== Testing Checklist ===" -ForegroundColor Cyan
Write-Host "[  ] Application running on localhost:5001" -ForegroundColor Yellow
Write-Host "[  ] ngrok tunnel active" -ForegroundColor Yellow
Write-Host "[  ] PayFast sandbox configured with ngrok URLs" -ForegroundColor Yellow
Write-Host "[  ] Test payment successful" -ForegroundColor Yellow
Write-Host "[  ] Webhook received and validated" -ForegroundColor Yellow
Write-Host "[  ] Database updated with payment status" -ForegroundColor Yellow
Write-Host "[  ] Return URL works" -ForegroundColor Yellow
Write-Host "[  ] Cancel URL works" -ForegroundColor Yellow
Write-Host ""

Write-Host "Useful Commands:" -ForegroundColor Cyan
Write-Host "  Monitor ngrok traffic: Visit http://127.0.0.1:4040" -ForegroundColor White
Write-Host "  Check application logs: Watch terminal output" -ForegroundColor White
Write-Host "  Query database: Use pgAdmin or psql" -ForegroundColor White
Write-Host ""

Write-Host "✓ Testing guide complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Update issue status:" -ForegroundColor Yellow
Write-Host "  bd update cse325t12Project-41v --notes ""Completed webhook testing successfully""" -ForegroundColor White
Write-Host "  bd close cse325t12Project-41v --reason ""All test scenarios passed""" -ForegroundColor White

# PayFast Integration Guide

## Overview
LoadHitch now integrates with PayFast, South Africa's leading payment gateway, for secure escrow-based payments. Customers pay for loads, funds are held in escrow, and released to drivers after successful delivery.

---

## 🔧 Configuration

### 1. Get PayFast Credentials

#### Sandbox (Testing):
- **Merchant ID**: `10000100`
- **Merchant Key**: `46f0cd694581a`
- **Passphrase**: (leave empty for sandbox)
- Already configured in `appsettings.json`

#### Production:
1. Sign up at https://www.payfast.co.za
2. Get your credentials from the PayFast dashboard
3. Update `appsettings.json` or Azure App Settings:

```json
{
  "PayFast": {
    "MerchantId": "your_merchant_id",
    "MerchantKey": "your_merchant_key",
    "Passphrase": "your_passphrase",
    "ProcessUrl": "https://www.payfast.co.za/eng/process",
    "ValidateUrl": "https://www.payfast.co.za/eng/query/validate",
    "UseSandbox": false
  }
}
```

### 2. Configure Webhook URL in PayFast Dashboard

Set your ITN (Instant Transaction Notification) URL:
```
https://loadhitch.azurewebsites.net/api/payfast/notify
```

**Important**: This URL must be publicly accessible for PayFast to send payment notifications.

---

## 🚀 How It Works

### Payment Flow:

1. **Customer Creates Load**
   - Enters load details (weight, category, pickup/delivery)
   - PricingService calculates price
   - Load saved with `PaymentId` = null

2. **Payment Initiation**
   ```csharp
   var payment = await _paymentService.InitiatePaymentAsync(loadId, customerId, amount);
   var request = await _paymentService.CreatePayFastPaymentAsync(payment.Id, baseUrl);
   ```

3. **Redirect to PayFast**
   - Customer redirected to PayFast payment page
   - Enters card details on PayFast (PCI compliant)
   - PayFast processes payment

4. **Payment Notification (ITN)**
   - PayFast sends webhook to `/api/payfast/notify`
   - PayFastController validates and processes payment
   - Payment status: Pending → Held (escrow)

5. **Delivery Confirmation**
   - Driver marks load as delivered
   - Customer confirms delivery
   - Payment released to driver

6. **Payment Release**
   ```csharp
   await _paymentService.ReleasePaymentToDriverAsync(paymentId, driverId);
   ```
   - Payment status: Held → Released
   - Driver receives 85% of payment
   - Platform retains 15% fee

---

## 📋 API Endpoints

### Create Payment
```http
POST /api/payment/initiate
Content-Type: application/json

{
  "loadId": "guid",
  "customerId": "string",
  "amount": 1500.00
}
```

### PayFast Webhook (ITN)
```http
POST /api/payfast/notify
Content-Type: application/x-www-form-urlencoded

(PayFast sends form data)
```

### Return URL (Success)
```http
GET /api/payfast/return?m_payment_id={guid}
```

### Cancel URL
```http
GET /api/payfast/cancel?m_payment_id={guid}
```

---

## 💻 Usage Examples

### Example 1: Create Payment in Controller

```csharp
[HttpPost("create-load-with-payment")]
public async Task<IActionResult> CreateLoadWithPayment([FromBody] CreateLoadRequest request)
{
    // 1. Calculate price
    var priceEstimate = await _pricingService.CalculatePriceAsync(new PriceCalculationRequest
    {
        PickupLat = request.PickupLatitude,
        PickupLng = request.PickupLongitude,
        DeliveryLat = request.DeliveryLatitude,
        DeliveryLng = request.DeliveryLongitude,
        LoadCategory = request.Category,
        WeightKg = request.WeightKg
    });

    // 2. Create load
    var load = new Load
    {
        Title = request.Title,
        Description = request.Description,
        WeightKg = request.WeightKg,
        LoadCategory = request.Category,
        CalculatedPrice = priceEstimate.TotalPrice,
        FinalPrice = request.CustomerOfferPrice ?? priceEstimate.TotalPrice,
        DriverEarnings = priceEstimate.DriverEarnings,
        CustomerId = GetUserId()
    };
    
    _context.Loads.Add(load);
    await _context.SaveChangesAsync();

    // 3. Initiate payment
    var payment = await _paymentService.InitiatePaymentAsync(
        load.Id, 
        load.CustomerId, 
        load.FinalPrice.Value
    );

    // 4. Create PayFast request
    var baseUrl = $"{Request.Scheme}://{Request.Host}";
    var payFastRequest = await _paymentService.CreatePayFastPaymentAsync(payment.Id, baseUrl);

    if (payFastRequest == null)
    {
        return BadRequest("Failed to create payment");
    }

    // 5. Return payment form HTML or redirect URL
    return Ok(new
    {
        loadId = load.Id,
        paymentId = payment.Id,
        payFastRequest = payFastRequest,
        payFastUrl = "https://sandbox.payfast.co.za/eng/process"
    });
}
```

### Example 2: Razor Page Payment Form

```razor
@page "/payment/{paymentId:guid}"
@inject PaymentService PaymentService
@inject NavigationManager Navigation

<div class="max-w-2xl mx-auto p-6">
    <h1 class="text-3xl font-bold mb-6">Complete Payment</h1>
    
    @if (_paymentRequest != null)
    {
        <PayFastPaymentForm 
            PaymentRequest="_paymentRequest" 
            ProcessUrl="@_processUrl"
            AutoSubmit="false" />
    }
</div>

@code {
    [Parameter]
    public Guid PaymentId { get; set; }

    private PayFastPaymentRequest? _paymentRequest;
    private string _processUrl = "https://sandbox.payfast.co.za/eng/process";

    protected override async Task OnInitializedAsync()
    {
        var baseUrl = Navigation.BaseUri.TrimEnd('/');
        _paymentRequest = await PaymentService.CreatePayFastPaymentAsync(PaymentId, baseUrl);
    }
}
```

---

## 🔒 Security Features

### 1. **Signature Verification**
Every PayFast notification includes an MD5 signature to prevent tampering:
```csharp
var isValid = _payFastService.VerifySignature(notification, signature);
```

### 2. **Server-Side Validation**
After signature check, we verify with PayFast servers:
```csharp
var isValid = await VerifyWithPayFastServerAsync(notification);
```

### 3. **Amount Verification**
We verify payment amounts match our records:
```csharp
if (notification.amount_gross != expectedAmount)
{
    return false; // Reject payment
}
```

### 4. **IP Whitelist** (Production)
Restrict webhook endpoint to PayFast IP addresses:
- 197.97.145.144
- 41.74.179.194
- 41.74.179.195
- 41.74.179.196
- 41.74.179.197

---

## 🧪 Testing

### Sandbox Testing Cards

#### Successful Payment:
- **Card Number**: 4000 0000 0000 0002
- **CVV**: Any 3 digits
- **Expiry**: Any future date

#### Failed Payment:
- **Card Number**: 4000 0000 0000 0010

### Test Payment Flow:
1. Create a load in development
2. Initiate payment
3. Use sandbox card details
4. Verify ITN webhook receives notification
5. Check payment status updated to "Held"
6. Simulate delivery
7. Release payment to driver

### Webhook Testing Locally:
Use ngrok to expose local endpoint:
```bash
ngrok http https://localhost:7218
# Update PayFast ITN URL to: https://your-ngrok-url.ngrok.io/api/payfast/notify
```

---

## 📊 Payment Statuses

| Status | Description |
|--------|-------------|
| `Pending` | Payment initiated, awaiting card details |
| `Held` | Payment successful, funds in escrow |
| `Released` | Funds transferred to driver after delivery |
| `Refunded` | Payment refunded to customer |
| `Failed` | Payment failed or declined |

---

## 💰 Fee Structure

- **Customer Pays**: Full load price (e.g., R1,500)
- **Platform Fee**: 15% (R225)
- **Driver Receives**: 85% (R1,275)
- **PayFast Fee**: ~2.9% + R1.50 (deducted from platform fee)

---

## 🚨 Error Handling

### Common Issues:

#### 1. **Invalid Signature**
```
Error: PayFast notification signature verification failed
Solution: Check passphrase matches in appsettings.json
```

#### 2. **Amount Mismatch**
```
Error: Payment amount mismatch
Solution: Ensure amount in database matches PayFast notification
```

#### 3. **Webhook Not Received**
```
Issue: Payment processed but status not updated
Solution: 
- Verify ITN URL is publicly accessible
- Check firewall/Azure NSG rules
- Enable PayFast IP whitelist
```

#### 4. **Duplicate Notifications**
PayFast may send multiple ITNs. Handle idempotently:
```csharp
if (payment.Status == PaymentStatus.Held)
{
    return Ok(); // Already processed
}
```

---

## 📈 Monitoring

### Key Metrics to Track:
- Payment success rate
- Average escrow hold duration
- Refund rate
- PayFast transaction fees
- Driver payout delays

### Logging:
All payment events are logged:
```csharp
_logger.LogInformation("Payment {PaymentId} completed. Amount: R{Amount}", 
    paymentId, amount);
```

Check logs in Application Insights or local logs.

---

## 🔄 Refund Process

### Automatic Refunds:
- Load cancelled before pickup
- Driver cancels after acceptance
- Customer dispute resolution

### Manual Refunds:
```csharp
await _paymentService.RefundPaymentAsync(paymentId, "Customer cancelled load");
```

Refunds processed within 5-7 business days to customer's card.

---

## 🌐 Production Deployment

### Pre-Deployment Checklist:
- [ ] Switch to production PayFast credentials
- [ ] Set `UseSandbox: false` in appsettings
- [ ] Configure public ITN webhook URL
- [ ] Test with real credit card (small amount)
- [ ] Verify SSL certificate is valid
- [ ] Add PayFast IP whitelist to firewall
- [ ] Set up monitoring alerts
- [ ] Test full payment → delivery → payout flow

### Azure App Settings:
```bash
az webapp config appsettings set --name loadhitch --resource-group LoadHitch --settings \
  PayFast__MerchantId="your_merchant_id" \
  PayFast__MerchantKey="your_merchant_key" \
  PayFast__Passphrase="your_passphrase" \
  PayFast__UseSandbox="false"
```

---

## 📞 Support

### PayFast Support:
- **Email**: support@payfast.co.za
- **Phone**: +27 21 827 0440
- **Documentation**: https://developers.payfast.co.za/

### LoadHitch Integration Issues:
- Check logs in Azure Application Insights
- Review payment status in database
- Verify webhook is receiving notifications
- Contact PayFast support for payment gateway issues

---

## 📝 Next Steps

1. **Complete UI Integration**:
   - Add payment form to load creation flow
   - Show payment status on customer dashboard
   - Display earnings on driver dashboard

2. **Add Payment History**:
   - Customer transaction history page
   - Driver earnings report
   - Export to CSV/PDF

3. **Enhance Notifications**:
   - SMS notifications for payment events
   - Email receipts after payment
   - WhatsApp delivery confirmations

4. **Analytics Dashboard**:
   - Revenue tracking
   - Payment success metrics
   - Driver payout statistics

---

**Status**: ✅ PayFast Integration Complete (Backend)  
**Testing**: 🧪 Ready for Sandbox Testing  
**Production**: 🚧 Requires Production Credentials  
**Documentation**: 📖 Complete

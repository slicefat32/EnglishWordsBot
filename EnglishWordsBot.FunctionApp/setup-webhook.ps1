# PowerShell script для налаштування Telegram Bot Webhook

param(
	[Parameter(Mandatory=$true)]
	[string]$BotToken,

	[Parameter(Mandatory=$true)]
	[string]$WebhookUrl
)

Write-Host "Setting up Telegram Bot Webhook..." -ForegroundColor Green
Write-Host "Bot Token: $BotToken"
Write-Host "Webhook URL: $WebhookUrl"

# Ensure webhook URL ends with /api/BotUpdate
if (-not $WebhookUrl.EndsWith("/api/BotUpdate")) {
	$WebhookUrl = $WebhookUrl.TrimEnd('/') + "/api/BotUpdate"
}

Write-Host "Full Webhook URL: $WebhookUrl" -ForegroundColor Yellow

# Set webhook
$setWebhookUrl = "https://api.telegram.org/bot$BotToken/setWebhook?url=$WebhookUrl"
Write-Host "`nSetting webhook..." -ForegroundColor Cyan

try {
	$response = Invoke-RestMethod -Uri $setWebhookUrl -Method Post

	if ($response.ok) {
		Write-Host "✓ Webhook set successfully!" -ForegroundColor Green
		Write-Host $response.description
	} else {
		Write-Host "✗ Error setting webhook:" -ForegroundColor Red
		Write-Host $response.description
		exit 1
	}
} catch {
	Write-Host "✗ Failed to set webhook: $_" -ForegroundColor Red
	exit 1
}

# Get webhook info
Write-Host "`nGetting webhook info..." -ForegroundColor Cyan
$getWebhookUrl = "https://api.telegram.org/bot$BotToken/getWebhookInfo"

try {
	$info = Invoke-RestMethod -Uri $getWebhookUrl -Method Get

	Write-Host "`nWebhook Information:" -ForegroundColor Yellow
	Write-Host "URL: $($info.result.url)"
	Write-Host "Pending Updates: $($info.result.pending_update_count)"

	if ($info.result.last_error_message) {
		Write-Host "Last Error: $($info.result.last_error_message)" -ForegroundColor Red
		Write-Host "Last Error Date: $($info.result.last_error_date)"
	} else {
		Write-Host "Status: OK ✓" -ForegroundColor Green
	}

	Write-Host "Allowed Updates: $($info.result.allowed_updates -join ', ')"

} catch {
	Write-Host "✗ Failed to get webhook info: $_" -ForegroundColor Red
}

Write-Host "`n✓ Setup complete!" -ForegroundColor Green
Write-Host "You can now test your bot by sending messages to it." -ForegroundColor Cyan


# OrderRequestQueueProcessor

A .NET background service that processes pending order requests from an Oracle queue, deserializes payloads, and updates their status based on processing results.

## Features
- Connects to an Oracle database and retrieves pending order requests.
- Deserializes JSON payloads containing one or more order requests.
- Processes each request using a custom handler.
- Updates the queue status to `Completed` or increments retry count on failure.
- Designed to run as a background service or console app on a local server.

## Requirements
- .NET 8.0 SDK
- Oracle Data Provider for .NET (ODP.NET)
- Windows Server (for deployment)
- Access to the Oracle database with appropriate credentials

## Configuration
Settings are stored in `appsettings.json`:

```json
{
  "BatchSize": 10,
  "PollingIntervalSeconds": 30,
  "ConnectionStrings": {
    "OracleDb": "your-oracle-connection-string"
  }
}
```

## Running Locally

### 🖥️ As a Console App on Startup

1. **Publish the app**:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```

2. **Copy the entire `publish` folder** to the server:
   ```
   C:\Apps\OrderRequestQueueProcessor   ```

3. **Create a shortcut** to `OrderRequestQueueProcessor.exe` in:
   ```
   C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup
   ```

   Or use **Task Scheduler** to run it at startup with elevated privileges.

## Logging
Logs are written using `ILogger`. For production, consider integrating:
- Serilog (file or rolling logs)
- Windows Event Log
- Application Insights (optional)

## Troubleshooting
- Ensure all files from the `publish` folder are copied — not just the `.exe`.
- If deserialization fails, check that the payload JSON matches the expected DTO structure.
- If the app fails to start, check for missing configuration or unresolved DI dependencies.

## Credits
Developed by Peter Holliday  
AAI.Systems Manager

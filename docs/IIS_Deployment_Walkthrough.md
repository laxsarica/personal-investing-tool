# Hangfire & IIS Setup Complete

I have fully implemented the Hangfire background job scheduler and created an automated script to deploy everything to your local IIS instance.

## 1. Hangfire Background Scheduler
The screener will no longer need manual triggering. It will run completely automatically on your local machine.

* **Database:** Hangfire uses your existing LocalDB to store jobs, history, and retries.
* **Dashboard:** A full UI is available at `/hangfire` to monitor job success/failure.
* **Scheduled Jobs:**
  1. **Daily Data Sync:** Runs every weekday at **6:30 AM IST** (`0 1 * * 1-5` UTC)
  2. **Daily Screener Run:** Runs every weekday at **7:00 AM IST** (`30 1 * * 1-5` UTC)

## 2. Automated IIS Deployment
Since running the API and Angular separately in the terminal every day is annoying, I've created an automated PowerShell script to host them permanently in IIS on your machine. 

Since your machine already has the **.NET 8 Hosting Bundle** installed, you just need to run the setup script.

### 🚀 How to Deploy (Do this now)

1. Open **PowerShell** as **Administrator** (Right-click Start -> Windows PowerShell (Admin)).
2. Run the deployment script I created:
```powershell
d:\MyStockMarketTool\setup_iis.ps1
```

> [!NOTE]
> The script will automatically:
> - Turn on all necessary IIS Windows Features
> - Build and Publish the .NET 8 API
> - Build the Angular 20 Frontend for production
> - Create IIS App Pools (configured to never sleep)
> - Create IIS Sites on Port 5100 (API) and Port 4200 (Angular)

### ✅ Verification

Once the script finishes:
1. Open [http://localhost:5100/hangfire](http://localhost:5100/hangfire) to view the new Background Jobs Dashboard.
2. Open [http://localhost:4200](http://localhost:4200) to view the Screener UI (now served fully through IIS, no terminal needed).

You can safely close all your terminals. The screener will now wake up every weekday morning and process the market data automatically in the background via IIS.

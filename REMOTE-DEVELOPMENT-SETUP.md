# Remote Development Setup Guide

Since the WPF application requires Windows to run, here are several options to develop and test remotely.

## Option 1: GitHub Codespaces (Recommended - Cloud-based)

### Setup:
1. Push your project to GitHub repository
2. Open GitHub repository in browser
3. Click "Code" → "Codespaces" → "Create codespace"
4. Install .NET 8.0 in the codespace:
   ```bash
   wget https://dot.net/v1/dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --version latest
   export PATH="$HOME/.dotnet:$PATH"
   ```

### Commands to run in Codespace:
```bash
# Clone and setup
git clone <your-repo-url>
cd WeighbridgeSoftwareYashCotex

# Build project
dotnet restore
dotnet build

# For Windows-specific testing, you can build and create deployment package
dotnet publish -c Release -r win-x64 --self-contained
```

## Option 2: Azure Cloud Shell / Azure VM

### Setup Azure VM:
1. Create Windows 11 VM on Azure
2. Enable RDP access
3. Connect via Remote Desktop

### Setup script for Azure VM:
```powershell
# Install .NET 8.0
Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/907765b0-2bf8-494e-93aa-5ef9553c5d68/a9308dc010617e6716c0e6abd53b05ce/dotnet-sdk-8.0.403-win-x64.exe" -OutFile "dotnet-sdk.exe"
Start-Process -FilePath "dotnet-sdk.exe" -ArgumentList "/quiet" -Wait

# Install Git
winget install Git.Git

# Install VS Code
winget install Microsoft.VisualStudioCode
```

## Option 3: Local Windows VM (VirtualBox/VMware)

### Setup:
1. Download Windows 11 development VM from Microsoft
2. Install VirtualBox or VMware on your Mac
3. Import the VM and start it
4. Follow the setup script above

## Option 4: SSH Tunnel to Windows Machine

If you have access to a Windows machine, set up SSH:

### On Windows machine:
```powershell
# Enable OpenSSH Server
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
Start-Service sshd
Set-Service -Name sshd -StartupType 'Automatic'

# Configure firewall
New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server (sshd)' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
```

### From your Mac:
```bash
# Connect to Windows machine
ssh username@windows-machine-ip

# Or use VS Code Remote SSH
code --install-extension ms-vscode-remote.remote-ssh
```

## Option 5: Docker with Windows Containers

### Dockerfile for Windows development:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022

WORKDIR /app
COPY . .

RUN dotnet restore
RUN dotnet build -c Release

EXPOSE 80
CMD ["dotnet", "run", "--configuration", "Release"]
```

### Build and run:
```bash
docker build -t weighbridge-app .
docker run -p 8080:80 weighbridge-app
```

## Option 6: Quick Setup Script for Any Windows Environment

Create this PowerShell script (`quick-setup.ps1`):
```powershell
# Quick Windows Development Setup
Write-Host "Setting up Weighbridge Software development environment..." -ForegroundColor Green

# Install Chocolatey (package manager)
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Install required tools
choco install -y dotnet-8.0-sdk
choco install -y git
choco install -y vscode

# Clone repository (you'll need to replace with your repo URL)
Write-Host "Please run: git clone <your-repo-url>" -ForegroundColor Yellow
Write-Host "Then navigate to the project directory and run:" -ForegroundColor Yellow
Write-Host "dotnet restore && dotnet build && dotnet run" -ForegroundColor Yellow
```

## Recommended Approach:

**For immediate testing:** Use GitHub Codespaces
1. Push your code to GitHub
2. Create a codespace
3. Build and test the logic (even if you can't run the GUI)

**For full Windows development:** Set up Azure Windows VM
1. Creates a proper Windows environment
2. Can run WPF applications
3. Accessible from anywhere

Would you like me to help you set up any of these options?
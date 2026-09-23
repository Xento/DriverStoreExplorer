using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace Rapr.Utils
{
    /// <summary>
    /// Read-only driver store backed by PowerShell remoting.
    /// The Driver Store Explorer process itself does not require elevation.
    /// Permissions are evaluated on the remote computer by the PowerShell remoting endpoint.
    /// </summary>
    public sealed class RemoteDriverStore : IDriverStore
    {
        public RemoteDriverStore(string computerName)
        {
            if (string.IsNullOrWhiteSpace(computerName))
            {
                throw new ArgumentException("A remote computer name is required.", nameof(computerName));
            }

            this.ComputerName = computerName.Trim();
        }

        public string ComputerName { get; }

        public DriverStoreType Type => DriverStoreType.Remote;

        // IDriverStore predates the remote store. Expose the target here so existing
        // title/status plumbing can still display a useful location.
        public string OfflineStoreLocation => this.ComputerName;

        public bool SupportAddInstall => false;

        public bool SupportForceDeletion => false;

        public bool SupportDeviceNameColumn => true;

        public bool SupportExportDriver => false;

        public bool SupportExportAllDrivers => false;

        public List<DriverStoreEntry> EnumeratePackages()
        {
            string json = this.RunRemoteInventory();

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<DriverStoreEntry>();
            }

            List<RemoteDriverInfo> remoteDrivers = JsonConvert.DeserializeObject<List<RemoteDriverInfo>>(json)
                ?? new List<RemoteDriverInfo>();

            List<DriverStoreEntry> result = new List<DriverStoreEntry>(remoteDrivers.Count);

            foreach (RemoteDriverInfo remoteDriver in remoteDrivers)
            {
                DateTime driverDate = default(DateTime);
                if (!string.IsNullOrWhiteSpace(remoteDriver.DriverDate))
                {
                    _ = DateTime.TryParse(
                        remoteDriver.DriverDate,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out driverDate);
                }

                Version driverVersion = null;
                if (!string.IsNullOrWhiteSpace(remoteDriver.DriverVersion))
                {
                    _ = Version.TryParse(remoteDriver.DriverVersion, out driverVersion);
                }

                result.Add(new DriverStoreEntry
                {
                    DriverPublishedName = remoteDriver.DriverPublishedName,
                    DriverInfName = remoteDriver.DriverInfName,
                    DriverPkgProvider = remoteDriver.DriverPkgProvider,
                    DriverClass = remoteDriver.DriverClass,
                    DriverDate = driverDate,
                    DriverVersion = driverVersion,
                    DriverSignerName = remoteDriver.DriverSignerName,
                    DriverSize = remoteDriver.DriverSize,
                    DriverFolderLocation = remoteDriver.DriverFolderLocation,
                    BootCritical = remoteDriver.BootCritical,
                    DeviceName = remoteDriver.DeviceName,
                    DevicePresent = remoteDriver.DevicePresent,
                });
            }

            return result;
        }

        public bool DeleteDriver(DriverStoreEntry driverStoreEntry, bool forceDelete)
        {
            throw new NotSupportedException("The remote driver store is read-only.");
        }

        public bool AddDriver(string infFullPath, bool install)
        {
            throw new NotSupportedException("The remote driver store is read-only.");
        }

        public bool ExportDriver(string infName, string destinationPath)
        {
            throw new NotSupportedException("The remote driver store is read-only.");
        }

        public bool ExportAllDrivers(string destinationPath)
        {
            throw new NotSupportedException("The remote driver store is read-only.");
        }

        private string RunRemoteInventory()
        {
            string escapedComputerName = this.ComputerName.Replace("'", "''");

            string script = @"
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$computerName = '" + escapedComputerName + @"'

try {
    $items = Invoke-Command -ComputerName $computerName -ErrorAction Stop -ScriptBlock {
        $ErrorActionPreference = 'Stop'

        $deviceDrivers = @()
        try {
            $deviceDrivers = @(Get-CimInstance -ClassName Win32_PnPSignedDriver -ErrorAction Stop)
        }
        catch {
            $deviceDrivers = @()
        }

        $presentDeviceIds = @{}
        try {
            Get-PnpDevice -PresentOnly -ErrorAction Stop | ForEach-Object {
                if ($_.InstanceId) {
                    $presentDeviceIds[[string]$_.InstanceId] = $true
                }
            }
        }
        catch {
            # Get-PnpDevice is optional. DevicePresent remains null if unavailable.
        }

        foreach ($driver in @(Get-WindowsDriver -Online -ErrorAction Stop)) {
            $publishedName = [System.IO.Path]::GetFileName([string]$driver.Driver)
            $originalFileName = [string]$driver.OriginalFileName
            $driverFolder = $null
            $driverSize = [int64]-1

            if ($originalFileName) {
                $driverFolder = Split-Path -LiteralPath $originalFileName -Parent

                if ($driverFolder -and (Test-Path -LiteralPath $driverFolder)) {
                    try {
                        $sum = (Get-ChildItem -LiteralPath $driverFolder -File -Recurse -Force -ErrorAction Stop |
                            Measure-Object -Property Length -Sum).Sum

                        if ($null -eq $sum) {
                            $driverSize = 0
                        }
                        else {
                            $driverSize = [int64]$sum
                        }
                    }
                    catch {
                        $driverSize = [int64]-1
                    }
                }
            }

            $device = $deviceDrivers |
                Where-Object {
                    $_.InfName -and
                    [string]::Equals(
                        [string]$_.InfName,
                        $publishedName,
                        [System.StringComparison]::OrdinalIgnoreCase)
                } |
                Select-Object -First 1

            $devicePresent = $null
            if ($device -and $device.DeviceID -and $presentDeviceIds.Count -gt 0) {
                $devicePresent = $presentDeviceIds.ContainsKey([string]$device.DeviceID)
            }

            $driverClass = [string]$driver.ClassDescription
            if (-not $driverClass) {
                $driverClass = [string]$driver.ClassName
            }

            $signer = $null
            if ($device -and $device.Signer) {
                $signer = [string]$device.Signer
            }
            elseif ($driver.DriverSignature) {
                $signer = [string]$driver.DriverSignature
            }

            [pscustomobject]@{
                DriverPublishedName = $publishedName
                DriverInfName = if ($originalFileName) { [System.IO.Path]::GetFileName($originalFileName) } else { $null }
                DriverPkgProvider = [string]$driver.ProviderName
                DriverClass = $driverClass
                DriverDate = if ($driver.Date) {
                    ([datetime]$driver.Date).ToString('o', [System.Globalization.CultureInfo]::InvariantCulture)
                } else {
                    $null
                }
                DriverVersion = if ($driver.Version) { [string]$driver.Version } else { $null }
                DriverSignerName = $signer
                DriverSize = $driverSize
                DriverFolderLocation = $driverFolder
                BootCritical = if ($null -ne $driver.BootCritical) { [bool]$driver.BootCritical } else { $null }
                DeviceName = if ($device) { [string]$device.DeviceName } else { $null }
                DevicePresent = $devicePresent
            }
        }
    }

    ConvertTo-Json -InputObject @($items) -Depth 4 -Compress
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
";

            string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoLogo -NoProfile -NonInteractive -EncodedCommand " + encodedCommand,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            Trace.TraceInformation($"Reading remote driver store from {this.ComputerName}");

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        throw new InvalidOperationException("Unable to start Windows PowerShell.");
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string detail = string.IsNullOrWhiteSpace(error)
                            ? $"PowerShell exited with code {process.ExitCode}."
                            : error.Trim();

                        throw new InvalidOperationException(
                            $"Unable to read the driver store from '{this.ComputerName}'. {detail}");
                    }

                    return output.Trim();
                }
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is InvalidOperationException
                || ex is System.ComponentModel.Win32Exception)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
        }

        private sealed class RemoteDriverInfo
        {
            public string DriverPublishedName { get; set; }

            public string DriverInfName { get; set; }

            public string DriverPkgProvider { get; set; }

            public string DriverClass { get; set; }

            public string DriverDate { get; set; }

            public string DriverVersion { get; set; }

            public string DriverSignerName { get; set; }

            public long DriverSize { get; set; }

            public string DriverFolderLocation { get; set; }

            public bool? BootCritical { get; set; }

            public string DeviceName { get; set; }

            public bool? DevicePresent { get; set; }
        }
    }
}

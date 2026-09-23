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
    /// Driver store backed by PowerShell remoting.
    /// The local Driver Store Explorer process can run unelevated. Administrative
    /// permissions for write operations are checked inside the remote session.
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

        // IDriverStore predates remote stores. Reuse this property for the target
        // name so the existing title/status plumbing can display the location.
        public string OfflineStoreLocation => this.ComputerName;

        public bool SupportAddInstall => true;

        public bool SupportForceDeletion => true;

        public bool SupportDeviceNameColumn => true;

        public bool SupportExportDriver => false;

        public bool SupportExportAllDrivers => true;

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
            if (driverStoreEntry == null)
            {
                throw new ArgumentNullException(nameof(driverStoreEntry));
            }

            if (string.IsNullOrWhiteSpace(driverStoreEntry.DriverPublishedName))
            {
                throw new ArgumentException("The driver package has no published INF name.", nameof(driverStoreEntry));
            }

            string publishedName = EscapePowerShellSingleQuotedString(driverStoreEntry.DriverPublishedName);
            string forceArgument = forceDelete ? " /uninstall /force" : string.Empty;

            string script = this.CreateSessionPrefix() + @"
try {
    Invoke-Command -Session $session -ErrorAction Stop -ScriptBlock {
        param($publishedName, $force)

        Assert-RemoteAdministrator

        $arguments = @('/delete-driver', $publishedName)
        if ($force) {
            $arguments += '/uninstall'
            $arguments += '/force'
        }

        $output = & pnputil.exe @arguments 2>&1
        $exitCode = $LASTEXITCODE

        if ($exitCode -ne 0) {
            throw ('PnPUtil failed with exit code {0}: {1}' -f $exitCode, ($output -join [Environment]::NewLine))
        }
    } -ArgumentList '" + publishedName + @"', $" + (forceDelete ? "true" : "false") + @"
}
finally {
    if ($session) {
        Remove-PSSession -Session $session -ErrorAction SilentlyContinue
    }
}
";

            this.RunPowerShell(script);
            return true;
        }

        public bool AddDriver(string infFullPath, bool install)
        {
            if (string.IsNullOrWhiteSpace(infFullPath))
            {
                throw new ArgumentException("An INF path is required.", nameof(infFullPath));
            }

            string fullInfPath = Path.GetFullPath(infFullPath);
            if (!File.Exists(fullInfPath))
            {
                throw new FileNotFoundException("The selected INF file does not exist.", fullInfPath);
            }

            string sourceFolder = Path.GetDirectoryName(fullInfPath);
            string infFileName = Path.GetFileName(fullInfPath);

            string escapedSourceFolder = EscapePowerShellSingleQuotedString(sourceFolder);
            string escapedInfFileName = EscapePowerShellSingleQuotedString(infFileName);

            string script = this.CreateSessionPrefix() + @"
$remoteStage = $null
try {
    Invoke-Command -Session $session -ErrorAction Stop -ScriptBlock {
        Assert-RemoteAdministrator
    }

    $remoteStage = Invoke-Command -Session $session -ErrorAction Stop -ScriptBlock {
        $path = Join-Path $env:TEMP ('DriverStoreExplorer-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $path -Force | Out-Null
        $path
    }

    $sourceFolder = '" + escapedSourceFolder + @"'
    Get-ChildItem -LiteralPath $sourceFolder -Force -ErrorAction Stop |
        Copy-Item -Destination $remoteStage -ToSession $session -Recurse -Force -ErrorAction Stop

    Invoke-Command -Session $session -ErrorAction Stop -ScriptBlock {
        param($stage, $infFileName, $install)

        Assert-RemoteAdministrator

        $remoteInf = Join-Path $stage $infFileName
        if (-not (Test-Path -LiteralPath $remoteInf -PathType Leaf)) {
            throw ('The staged INF file was not found: ' + $remoteInf)
        }

        $arguments = @('/add-driver', $remoteInf)
        if ($install) {
            $arguments += '/install'
        }

        $output = & pnputil.exe @arguments 2>&1
        $exitCode = $LASTEXITCODE

        if ($exitCode -ne 0) {
            throw ('PnPUtil failed with exit code {0}: {1}' -f $exitCode, ($output -join [Environment]::NewLine))
        }
    } -ArgumentList $remoteStage, '" + escapedInfFileName + @"', $" + (install ? "true" : "false") + @"
}
finally {
    if ($session) {
        if ($remoteStage) {
            Invoke-Command -Session $session -ScriptBlock {
                param($path)
                Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
            } -ArgumentList $remoteStage -ErrorAction SilentlyContinue
        }

        Remove-PSSession -Session $session -ErrorAction SilentlyContinue
    }
}
";

            this.RunPowerShell(script);
            return true;
        }

        public bool ExportDriver(string infName, string destinationPath)
        {
            throw new NotSupportedException("Exporting a single driver from a remote store is not supported.");
        }

        public bool ExportAllDrivers(string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("A destination path is required.", nameof(destinationPath));
            }

            Directory.CreateDirectory(destinationPath);

            string escapedDestination = EscapePowerShellSingleQuotedString(Path.GetFullPath(destinationPath));

            string script = this.CreateSessionPrefix() + @"
$remoteStage = $null
try {
    Invoke-Command -Session $session -ErrorAction Stop -ScriptBlock {
        Assert-RemoteAdministrator
    }

    $remoteStage = Invoke-Command -Session $session -ErrorAction Stop -ScriptBlock {
        $path = Join-Path $env:TEMP ('DriverStoreExplorer-Export-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $path -Force | Out-Null
        Export-WindowsDriver -Online -Destination $path -ErrorAction Stop | Out-Null
        $path
    }

    $destination = '" + escapedDestination + @"'
    Get-ChildItem -Path (Join-Path $remoteStage '*') -FromSession $session -Force -ErrorAction Stop |
        Copy-Item -Destination $destination -Recurse -Force -ErrorAction Stop
}
finally {
    if ($session) {
        if ($remoteStage) {
            Invoke-Command -Session $session -ScriptBlock {
                param($path)
                Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
            } -ArgumentList $remoteStage -ErrorAction SilentlyContinue
        }

        Remove-PSSession -Session $session -ErrorAction SilentlyContinue
    }
}
";

            this.RunPowerShell(script);
            return true;
        }

        private string RunRemoteInventory()
        {
            string script = this.CreateSessionPrefix() + @"
try {
    $items = Invoke-Command -Session $session -ErrorAction Stop -ScriptBlock {
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
finally {
    if ($session) {
        Remove-PSSession -Session $session -ErrorAction SilentlyContinue
    }
}
";

            return this.RunPowerShell(script).Trim();
        }

        private string CreateSessionPrefix()
        {
            string escapedComputerName = EscapePowerShellSingleQuotedString(this.ComputerName);

            return @"
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

function Assert-RemoteAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)

    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'The PowerShell remoting session does not have administrator rights on the target computer.'
    }
}

$session = $null
$session = New-PSSession -ComputerName '" + escapedComputerName + @"' -ErrorAction Stop
";
        }

        private string RunPowerShell(string script)
        {
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

            Trace.TraceInformation($"Executing remote driver store operation on {this.ComputerName}");

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
                            $"Remote operation on '{this.ComputerName}' failed. {detail}");
                    }

                    return output;
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

        private static string EscapePowerShellSingleQuotedString(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
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

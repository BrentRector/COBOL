# PB1708 pivot evidence b6 — sample the newest testhost's cumulative CPU and the host's CPU every 10 s while a
# run-leg.ps1 run is in flight (start that run first, in another shell). Stops when the testhost exits.
#     pwsh -File sample-cpu.ps1 -Label E1b [-Out <dir>]
param([Parameter(Mandatory)][string]$Label, [int]$Seconds = 900,
      [string]$Out = (Join-Path ([IO.Path]::GetTempPath()) 'pb1708-b6'))
New-Item -ItemType Directory -Force $Out | Out-Null
$file = Join-Path $Out "$Label.cpu.txt"
"t_s host_cpu_pct testhost_cpu_s testhost_threads" | Set-Content $file
$t0 = Get-Date
$seen = $false
while (((Get-Date) - $t0).TotalSeconds -lt $Seconds) {
    $h = (Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 1 -MaxSamples 1).CounterSamples[0].CookedValue
    $p = Get-Process testhost -ErrorAction SilentlyContinue | Sort-Object StartTime -Descending | Select-Object -First 1
    if ($p) {
        $seen = $true
        "{0:F0} {1:F1} {2:F1} {3}" -f ((Get-Date) - $t0).TotalSeconds, $h, $p.TotalProcessorTime.TotalSeconds, $p.Threads.Count | Add-Content $file
    }
    elseif ($seen) { break }
    Start-Sleep -Seconds 9
}

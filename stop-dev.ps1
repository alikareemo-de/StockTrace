$ports = @(5133, 5173)

foreach ($port in $ports) {
    $processIds = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue |
        Where-Object { $_.State -eq "Listen" } |
        Select-Object -ExpandProperty OwningProcess -Unique

    foreach ($processId in $processIds) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($process) {
            Write-Host "Stopping $($process.ProcessName) on port $port (PID $processId)..."
            Stop-Process -Id $processId -Force
        }
    }
}

Write-Host "Development processes stopped."

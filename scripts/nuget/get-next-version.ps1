param(
    [Parameter(Mandatory = $true)] [string]$PackageName,
    [string]$Fallback = "1.0.0"
)

# Devuelve por stdout SOLO la version calculada (X.Y.Z).
# Consulta el flat container de nuget.org (no requiere API key para lectura).
# Cualquier mensaje de diagnostico se escribe a stderr para no romper el FOR /F del .bat.

function Write-Info($msg) { [Console]::Error.WriteLine("[get-next-version] $msg") }

try {
    $idLower = $PackageName.ToLowerInvariant()
    $uri = "https://api.nuget.org/v3-flatcontainer/$idLower/index.json"

    $resp = Invoke-RestMethod -Uri $uri -ErrorAction Stop

    if ($null -eq $resp -or $null -eq $resp.versions -or $resp.versions.Count -eq 0) {
        Write-Info "Sin versiones previas para '$PackageName'. Usando fallback $Fallback."
        return $Fallback
    }

    $stable = @()
    foreach ($v in $resp.versions) {
        if ($v -match '^\d+\.\d+\.\d+$') { $stable += $v }
    }

    if ($stable.Count -eq 0) {
        Write-Info "No hay versiones estables X.Y.Z para '$PackageName'. Usando fallback $Fallback."
        return $Fallback
    }

    $latest = $stable | Sort-Object { [version]$_ } -Descending | Select-Object -First 1

    $parts = $latest.Split('.')
    $parts[2] = [string]([int]$parts[2] + 1)
    $next = ($parts -join '.')

    Write-Info "Ultima publicada de '$PackageName' = $latest -> proxima = $next"
    return $next
}
catch {
    # 404 del flat container = paquete nunca publicado
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode.value__ -eq 404) {
        Write-Info "Paquete '$PackageName' no existe aun en nuget.org. Usando fallback $Fallback."
        return $Fallback
    }
    Write-Info "ERROR consultando nuget.org para '$PackageName': $($_.Exception.Message). Usando fallback $Fallback."
    return $Fallback
}

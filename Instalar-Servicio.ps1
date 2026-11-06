<#
  Instala (o actualiza) MailDispatcher.Worker como servicio de Windows. Ejecutar como administrador en el servidor,
  desde la carpeta publicada (la que tiene MailDispatcher.Worker.exe).

  Ejemplos:
    .\Instalar-Servicio.ps1 -Entorno Testing                 # servidor de pruebas
    .\Instalar-Servicio.ps1 -Entorno Development             # servidor de desarrollo
    .\Instalar-Servicio.ps1 -Entorno Testing -Desinstalar    # detiene y borra el servicio (no borra la carpeta)

  - El entorno se fija en el propio servicio (DOTNET_ENVIRONMENT), sin depender de las variables de la maquina.
    El Worker lee appsettings.json y appsettings.<Entorno>.json.
  - Fuera de Production el Worker SOLO envia a Destinatarios:Permitidos de appsettings.<Entorno>.json; al resto de
    los mails los deja en estado 151 (No Enviado). Revisar esa lista antes de iniciar.
  - Al actualizar no se pisan los appsettings que ya estan en la carpeta de instalacion (tienen la configuracion del
    servidor: conexiones, destinatarios). Usar -SobrescribirConfiguracion para reemplazarlos.
#>
param(
  [Parameter(Mandatory = $true)]
  [ValidateSet('Development', 'Testing', 'Production')]
  [string]$Entorno,
  [string]$Carpeta = 'C:\Servicios\MailDispatcher',
  [string]$NombreServicio = 'MailDispatcherWorker',
  [string]$Origen = $PSScriptRoot,
  [switch]$SobrescribirConfiguracion,
  [switch]$NoIniciar,
  [switch]$Desinstalar
)

$ErrorActionPreference = 'Stop'

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) { throw 'Ejecutar como administrador.' }

$servicio = Get-Service -Name $NombreServicio -ErrorAction SilentlyContinue

if ($servicio -and $servicio.Status -ne 'Stopped') {
  Write-Host "Deteniendo $NombreServicio..."
  Stop-Service -Name $NombreServicio -Force
  $servicio.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(60))
}

if ($Desinstalar) {
  if ($servicio) { sc.exe delete $NombreServicio | Out-Null; Write-Host "Servicio $NombreServicio eliminado." }
  else { Write-Host "El servicio $NombreServicio no existe." }
  return
}

$exeOrigen = Join-Path $Origen 'MailDispatcher.Worker.exe'
if (-not (Test-Path $exeOrigen)) { throw "No encuentro MailDispatcher.Worker.exe en '$Origen'. Ejecutar desde la carpeta publicada o pasar -Origen." }

# Copia de archivos (sin pisar la configuracion del servidor)
New-Item -ItemType Directory -Force -Path $Carpeta | Out-Null
if ((Resolve-Path $Origen).Path.TrimEnd('\') -ne (Resolve-Path $Carpeta).Path.TrimEnd('\')) {
  foreach ($archivo in Get-ChildItem -Path $Origen -File) {
    if ($archivo.Name -eq 'Instalar-Servicio.ps1') { continue }
    $destino = Join-Path $Carpeta $archivo.Name
    if ($archivo.Name -like 'appsettings*.json' -and (Test-Path $destino) -and -not $SobrescribirConfiguracion) {
      Write-Host "Se conserva la configuracion existente: $($archivo.Name)"
      continue
    }
    Copy-Item -Path $archivo.FullName -Destination $destino -Force
  }
}

$exe = Join-Path $Carpeta 'MailDispatcher.Worker.exe'
$config = Join-Path $Carpeta "appsettings.$Entorno.json"
if (-not (Test-Path $config)) { throw "Falta $config (conexiones y destinatarios de este ambiente)." }

# Control previo: en desarrollo y pruebas mostrar a quien se le va a poder enviar
$cfg = Get-Content $config -Raw | ConvertFrom-Json
if ($Entorno -ne 'Production') {
  $permitidos = @($cfg.Destinatarios.Permitidos)
  if ($permitidos.Count -eq 0) { Write-Warning "Destinatarios:Permitidos esta vacio en $config. El servicio no va a enviar ningun mail." }
  else { Write-Host "Solo se va a enviar a: $($permitidos -join ', ')" }
}
if ($cfg.Worker.RunOnce -eq $true) { Write-Warning "Worker:RunOnce = true en ${config}: el servicio procesa una vez y se detiene." }

# Alta o actualizacion del servicio
if (-not $servicio) {
  New-Service -Name $NombreServicio -BinaryPathName "`"$exe`"" -DisplayName 'Mail Dispatcher Worker' `
    -Description 'Envia los mails pendientes de AudMailsEnvios (EVOL).' -StartupType Automatic | Out-Null
  Write-Host "Servicio $NombreServicio creado."
} else {
  sc.exe config $NombreServicio binPath= "`"$exe`"" start= auto | Out-Null
  Write-Host "Servicio $NombreServicio actualizado."
}

# Entorno propio del servicio (REG_MULTI_SZ "Environment" del servicio)
$reg = "HKLM:\SYSTEM\CurrentControlSet\Services\$NombreServicio"
New-ItemProperty -Path $reg -Name 'Environment' -PropertyType MultiString -Value @("DOTNET_ENVIRONMENT=$Entorno") -Force | Out-Null

# Si se cae, Windows lo reinicia (a los 60 segundos)
sc.exe failure $NombreServicio reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

if (-not $NoIniciar) {
  Start-Service -Name $NombreServicio
  (Get-Service -Name $NombreServicio).WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
}

Get-Service -Name $NombreServicio | Format-Table Name, Status, StartType -AutoSize
Write-Host "Entorno: $Entorno - Carpeta: $Carpeta"

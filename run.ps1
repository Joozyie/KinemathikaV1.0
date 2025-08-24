$envPath = Join-Path $PSScriptRoot ".env"                       # .env beside this script
$proj    = "E:\kinemathika\kinemathikaV1\kinemathika\kinemathika.csproj"  # <-- your .csproj

# load key=value lines (skip comments/blank)
Get-Content $envPath |
  Where-Object { $_ -match "^\s*[^#].+=.+$" } |
  ForEach-Object {
    $k,$v = ($_ -split "=",2); $k=$k.Trim(); $v=$v.Trim().Trim("'`"")
    Set-Item -Path "Env:$k" -Value $v
  }

dotnet run --project $proj

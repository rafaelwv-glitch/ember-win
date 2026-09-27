$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
dotnet publish "$root\src\Ember.App\Ember.App.csproj" -c Release -r win-x64 --self-contained true
Write-Host "Published under src\Ember.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"

param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'artifacts')
)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'scripts\check.ps1')
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $frameworkRoot 'csc.exe'))) {
    $frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
}
$compiler = Join-Path $frameworkRoot 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework C# compiler was not found. Run this script on Windows with .NET Framework 4.8 installed.'
}

$source = Join-Path $PSScriptRoot 'src\NightCue'
$testSource = Join-Path $PSScriptRoot 'tests\NightCue.Tests\Program.cs'
$wpf = Join-Path $frameworkRoot 'WPF'
$appDir = Join-Path $OutputPath 'app'
$testDir = Join-Path $OutputPath 'tests'
New-Item -ItemType Directory -Path $appDir, $testDir -Force | Out-Null
$application = Join-Path $appDir 'NightCue.exe'
$testExecutable = Join-Path $testDir 'NightCue.Tests.exe'
$references = @(
    '/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.Xaml.dll',
    "/r:$wpf\WindowsBase.dll", "/r:$wpf\PresentationFramework.dll",
    "/r:$wpf\PresentationCore.dll", "/r:$wpf\WindowsFormsIntegration.dll"
)

$languageResources = @(Get-ChildItem -LiteralPath (Join-Path $source 'Localization\Languages') -Filter '*.xml' -File | ForEach-Object {
    '/resource:{0},NightCue.Languages.{1}' -f $_.FullName, $_.Name
})
[xml]$project = Get-Content -LiteralPath (Join-Path $source 'NightCue.csproj') -Raw
$compileSources = @($project.Project.ItemGroup.Compile | Where-Object { $_.Include } | ForEach-Object { Join-Path $source $_.Include })
& $compiler /nologo /codepage:65001 /target:winexe /optimize+ /platform:anycpu `
    "/win32icon:$source\Assets\NightCue.ico" `
    "/win32manifest:$source\Assets\NightCue.manifest" `
    "/resource:$source\Shell\SettingsView.xaml,NightCue.SettingsView.xaml" `
    /warnaserror+ "/out:$application" @references @languageResources @compileSources
if ($LASTEXITCODE -ne 0) { throw "Application build failed (exit $LASTEXITCODE)." }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\使用说明.txt') -Destination $appDir -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\QUICKSTART.en.txt') -Destination $appDir -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination $appDir -Force

& $compiler /nologo /codepage:65001 /warnaserror+ /target:exe "/out:$testExecutable" "/r:$application" @references $testSource
if ($LASTEXITCODE -ne 0) { throw "Test build failed (exit $LASTEXITCODE)." }
Copy-Item -LiteralPath $application -Destination $testDir -Force
& $testExecutable
if ($LASTEXITCODE -ne 0) { throw "Tests failed (exit $LASTEXITCODE)." }

Write-Output "Build and tests passed. Application: $application"
Write-Output 'The application was not started; no Windows settings were changed.'

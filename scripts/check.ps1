param()

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$version = (Get-Content -LiteralPath (Join-Path $repo 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must contain a stable semantic version.' }

$required = @(
    'LICENSE', 'README.md', 'README.zh-CN.md', 'CONTRIBUTING.md', 'CODE_OF_CONDUCT.md',
    'SECURITY.md', 'CHANGELOG.md', 'NightCue.sln', '.gitignore', '.gitattributes', '.editorconfig',
    'docs/QUICKSTART.en.txt', 'docs/使用说明.txt', 'docs/PUBLISHING.md', 'docs/VALIDATION.md',
    '.github/workflows/build.yml', '.github/ISSUE_TEMPLATE/bug_report.yml',
    '.github/ISSUE_TEMPLATE/feature_request.yml', '.github/PULL_REQUEST_TEMPLATE.md'
)
foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $repo $name) -PathType Leaf)) { throw "Missing file: $name" }
}
$assemblyInfo = Get-Content -LiteralPath (Join-Path $repo 'src/NightCue/Properties/AssemblyInfo.cs') -Raw
foreach ($attribute in @('AssemblyVersion', 'AssemblyFileVersion')) {
    if ($assemblyInfo -notmatch ($attribute + '\("' + [regex]::Escape($version) + '\.0"\)')) {
        throw "$attribute does not match VERSION."
    }
}
[xml]$manifest = Get-Content -LiteralPath (Join-Path $repo 'src/NightCue/Assets/NightCue.manifest') -Raw
if ($manifest.assembly.assemblyIdentity.version -ne "$version.0") { throw 'Manifest version does not match VERSION.' }
foreach ($name in @('README.md', 'README.zh-CN.md', 'docs/RELEASE_NOTES.md')) {
    if ((Get-Content -LiteralPath (Join-Path $repo $name) -Raw) -notmatch [regex]::Escape("NightCue-v$version-windows-portable.zip")) {
        throw "Package version missing or outdated in $name"
    }
}

foreach ($projectPath in @('src/NightCue/NightCue.csproj', 'tests/NightCue.Tests/NightCue.Tests.csproj')) {
    $projectFile = Join-Path $repo $projectPath
    $projectDir = Split-Path $projectFile -Parent
    [xml]$project = Get-Content -LiteralPath $projectFile -Raw
    $namespace = New-Object System.Xml.XmlNamespaceManager($project.NameTable)
    $namespace.AddNamespace('m', 'http://schemas.microsoft.com/developer/msbuild/2003')
    $included = @($project.SelectNodes('//m:Compile', $namespace) | ForEach-Object {
        $file = [IO.Path]::GetFullPath((Join-Path $projectDir $_.Include))
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing compile item: $file" }
        $file
    })
    $actual = @(Get-ChildItem -LiteralPath $projectDir -Filter '*.cs' -File -Recurse | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    } | Select-Object -ExpandProperty FullName)
    if (Compare-Object $included $actual) { throw "C# file inclusion mismatch: $projectPath" }
}
[xml]$mainProject = Get-Content -LiteralPath (Join-Path $repo 'src/NightCue/NightCue.csproj') -Raw
$embedded = @($mainProject.Project.ItemGroup.EmbeddedResource | Where-Object { $_.Include -eq 'Shell\SettingsView.xaml' })
if ($embedded.Count -ne 1 -or $embedded[0].LogicalName -ne 'NightCue.SettingsView.xaml') {
    throw 'SettingsView.xaml must retain its embedded resource name.'
}

# Check relative Markdown links, including screenshots, without network access.
$markdown = @(Get-ChildItem -LiteralPath $repo -Filter '*.md' -File)
$markdown += @(Get-ChildItem -LiteralPath (Join-Path $repo 'docs') -Filter '*.md' -File -Recurse)
foreach ($file in $markdown) {
    $body = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in [regex]::Matches($body, '!?(?:\[[^\]]*\])\(([^)]+)\)')) {
        $target = $match.Groups[1].Value
        if ($target -match '^(https?://|mailto:|#)') { continue }
        $target = [Uri]::UnescapeDataString(($target -split '#', 2)[0])
        if (-not (Test-Path -LiteralPath (Join-Path $file.DirectoryName $target))) {
            throw "Broken documentation link in $($file.Name): $target"
        }
    }
}

$allowlistPath = Join-Path $PSScriptRoot 'source-files.txt'
$files = @(Get-Content -LiteralPath $allowlistPath | Where-Object { $_ -and -not $_.StartsWith('#') })
if (@($files | Sort-Object -Unique).Count -ne $files.Count) { throw 'Duplicate source allowlist entry.' }
$repoPrefix = [IO.Path]::GetFullPath($repo).TrimEnd('\') + '\'
foreach ($name in $files) {
    $fullPath = [IO.Path]::GetFullPath((Join-Path $repo $name))
    if (-not $fullPath.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe package path: $name" }
    if ($name -match '(^|/)(settings\.xml|state\.xml|error\.log|\.env|bin|obj|artifacts|\.git)(/|$)|\.(exe|dll|pdb|zip|tmp|log)$') {
        throw "Runtime/build data in source allowlist: $name"
    }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { throw "Missing source package item: $name" }
}
Write-Output "Repository checks passed ($version; $($files.Count) source files)."

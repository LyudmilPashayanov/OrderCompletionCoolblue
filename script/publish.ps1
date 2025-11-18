$solutionRoot = ".."
$outputFolder = "$solutionRoot\script\output"
$zipFilePath = "$outputFolder\csharp-assignment.zip"

if (-not (Test-Path -Path $outputFolder)) {
    New-Item -Path $outputFolder -ItemType Directory -Force | Out-Null
}

if (Test-Path -Path $zipFilePath) {
    Remove-Item -Path $zipFilePath -Force
}

$itemsToInclude = Get-ChildItem -Path $solutionRoot -Recurse -Force |
    Where-Object { $_.FullName -notlike "*\.vs*" -and $_.FullName -notlike "*NCrunch*" -and $_.FullName -notlike "*\.git*" -and $_.FullName -notlike "*\script*" -and $_.FullName -notlike "*\.github*" -and $_.FullName -notlike "*\bin*" -and $_.FullName -notlike "*\obj*" -and $_.Name -ne "Interviewer_notes.md" -and $_.Name -ne "PULL_REQUEST_TEMPLATE.md" }

$tempFolder = "$outputFolder\temp"
if (Test-Path -Path $tempFolder) {
    Remove-Item -Path $tempFolder -Recurse -Force
}
New-Item -Path $tempFolder -ItemType Directory | Out-Null

foreach ($item in $itemsToInclude) {
    $relativePath = $item.FullName.Substring((Get-Item -Path $solutionRoot).FullName.Length + 1)
    $destination = "$tempFolder\$relativePath"
    if ($item.PSIsContainer) {
        New-Item -Path $destination -ItemType Directory -Force | Out-Null
    } else {
        Copy-Item -Path $item.FullName -Destination $destination -Force
    }
}

Compress-Archive -Path "$tempFolder\*" -DestinationPath $zipFilePath -CompressionLevel Optimal

Remove-Item -Path $tempFolder -Recurse -Force

Write-Output "Zip file created at: $zipFilePath"
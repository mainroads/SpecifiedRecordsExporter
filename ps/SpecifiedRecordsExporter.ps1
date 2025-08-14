# Specified Records Exporter PowerShell Script

<#!
Interactive script to replicate functionality of the Specified Records Exporter application.
It removes junk files, unzips non-CAD archives, zips folders containing CAD files,
renames files based on their folder structure and moves them to the top level folder.
#>

param()

$JunkFilesList = @('.DS_Store', 'TRIMfiles.dat')
$NonCadFileExtensions = @('pdf', 'docx')
$CadFileExtensions = @('dwg', 'shp', 'sor')
$CadPatterns = $CadFileExtensions | ForEach-Object { "*.${_}" }

function Get-CleanFileName {
    param([string]$Name)
    $invalid = [IO.Path]::GetInvalidFileNameChars()
    return -join ($Name.ToCharArray() | ForEach-Object { if ($invalid -contains $_) { '_' } else { $_ } })
}

function Invoke-WithRetry {
    param(
        [scriptblock]$Action,
        [int]$IntervalMs = 250,
        [int]$TimeoutMs = 5000
    )
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $TimeoutMs) {
        try {
            if (& $Action) { return $true }
        } catch {}
        Start-Sleep -Milliseconds $IntervalMs
    }
    return $false
}

function Get-UniqueFilePath {
    param([string]$Path)
    $dir = [IO.Path]::GetDirectoryName($Path)
    $name = [IO.Path]::GetFileNameWithoutExtension($Path)
    $ext = [IO.Path]::GetExtension($Path)
    $i = 1
    $unique = $Path
    while (Test-Path -LiteralPath $unique) {
        $unique = Join-Path $dir ("{0} ({1}){2}" -f $name,$i,$ext)
        $i++
    }
    return $unique
}

function Get-DestPath {
    param(
        [string]$OriginalPath,
        [string]$RootDir,
        [string]$FreeText
    )
    $relative = $OriginalPath.Substring($RootDir.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
    $relative = $relative -replace [regex]::Escape([IO.Path]::DirectorySeparatorChar), ' - '
    if ($FreeText) { $newName = "$FreeText - $relative" } else { $newName = $relative }
    $newName = $newName -replace '\s*-\s*$', ''
    $destPath = Join-Path $RootDir (Get-CleanFileName $newName)
    if ($destPath.Length -gt 260) {
        $diff = $destPath.Length - 260
        $base = [IO.Path]::GetFileNameWithoutExtension($destPath)
        $ext  = [IO.Path]::GetExtension($destPath)
        $base = $base.Substring(0, [Math]::Max(0, $base.Length - $diff))
        $destPath = Join-Path ([IO.Path]::GetDirectoryName($destPath)) ($base + $ext)
    }
    $file = Get-CleanFileName ([IO.Path]::GetFileName($destPath))
    $destPath = Join-Path ([IO.Path]::GetDirectoryName($destPath)) $file
    return Get-UniqueFilePath $destPath
}

function Remove-JunkFiles {
    param([string]$RootDir)
    $files = Get-ChildItem -Path $RootDir -File -Recurse -Force
    foreach ($file in $files) {
        if ($JunkFilesList -contains $file.Name -or $JunkFilesList -contains $file.Extension) {
            Write-Host "Removing $($file.FullName)" -ForegroundColor Yellow
            Invoke-WithRetry { Remove-Item -LiteralPath $file.FullName -Force; return $true } | Out-Null
        } else {
            Write-Host "Preview: $($file.FullName)"
        }
    }
}

function Move-NonCadFilesOutOfCadFolder {
    param([string]$CadFolder)
    $moved = $false
    foreach ($ext in $NonCadFileExtensions) {
        $files = Get-ChildItem -Path $CadFolder -Filter "*.${ext}" -File -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            $dest = Join-Path (Split-Path $CadFolder -Parent) $file.Name
            Move-Item -LiteralPath $file.FullName -Destination $dest -Force
            $moved = $true
        }
    }
    return $moved
}

function Unzip-NonCadFiles {
    param([string]$RootDir)
    while ((Get-ChildItem -Path $RootDir -Filter '*.zip' -Recurse -File).Count -gt 0) {
        foreach ($zip in Get-ChildItem -Path $RootDir -Filter '*.zip' -Recurse -File) {
            $zipDir = Join-Path $zip.DirectoryName $($zip.BaseName)
            $destination = if ($zip.FullName.Length -gt 200) { $RootDir } else { $zipDir }
            $extracted = $true
            try {
                Expand-Archive -Path $zip.FullName -DestinationPath $destination -Force
            } catch {
                try {
                    Expand-Archive -Path $zip.FullName -DestinationPath $destination
                } catch {
                    $corrupted = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'Downloads/Corrupted Records'
                    New-Item -ItemType Directory -Path $corrupted -Force | Out-Null
                    Move-Item -LiteralPath $zip.FullName -Destination (Join-Path $corrupted $zip.Name) -Force
                    $extracted = $false
                }
            }
            if (-not $extracted) { continue }
            if (Test-Path -LiteralPath $zipDir) {
                $innerZips = Get-ChildItem -Path $zipDir -Filter '*.zip' -Recurse -File -ErrorAction SilentlyContinue
                foreach ($inner in $innerZips) {
                    Move-Item -LiteralPath $inner.FullName -Destination (Join-Path $RootDir $inner.Name) -Force
                }
                $hasCad = $false
                foreach ($pattern in $CadPatterns) {
                    if (Get-ChildItem -Path $zipDir -Filter $pattern -Recurse -ErrorAction SilentlyContinue) {
                        $hasCad = $true; break
                    }
                }
                if ($hasCad) {
                    if (Move-NonCadFilesOutOfCadFolder $zipDir) {
                        Invoke-WithRetry { Remove-Item -LiteralPath $zip.FullName -Force; return $true } | Out-Null
                        Compress-Archive -Path $zipDir -DestinationPath $zip.FullName -Force
                    }
                    Invoke-WithRetry { Remove-Item -Path $zipDir -Recurse -Force; return $true } | Out-Null
                } else {
                    Invoke-WithRetry { Remove-Item -LiteralPath $zip.FullName -Force; return $true } | Out-Null
                }
                if (Test-Path -LiteralPath $zipDir) {
                    Unzip-NonCadFiles $zipDir
                }
            } else {
                Invoke-WithRetry { Remove-Item -LiteralPath $zip.FullName -Force; return $true } | Out-Null
            }
        }
    }
}

function Zip-CadFolders {
    param([string]$Folder)
    $hasCad = $false
    foreach ($pattern in $CadPatterns) {
        if (Get-ChildItem -Path $Folder -Filter $pattern -File -Recurse -ErrorAction SilentlyContinue) {
            $hasCad = $true; break
        }
    }
    if ($hasCad) {
        Move-NonCadFilesOutOfCadFolder $Folder | Out-Null
        $zipName = Split-Path $Folder -Leaf
        if (-not ($zipName -match 'CAD$')) { $zipName = "$zipName CAD" }
        $zipPath = Join-Path (Split-Path $Folder -Parent) ("$zipName.zip")
        Write-Host "Zipping $Folder" -ForegroundColor Cyan
        Compress-Archive -Path $Folder -DestinationPath $zipPath -Force
        Invoke-WithRetry { Remove-Item -Path $Folder -Recurse -Force; return $true } | Out-Null
    } else {
        foreach ($sub in Get-ChildItem -Path $Folder -Directory) {
            Zip-CadFolders $sub.FullName
        }
    }
}

function Rename-Files {
    param([string]$RootDir, [string]$FreeText)
    $files = Get-ChildItem -Path $RootDir -File -Recurse
    $count = $files.Count
    $i = 0
    foreach ($file in $files) {
        $dest = Get-DestPath -OriginalPath $file.FullName -RootDir $RootDir -FreeText $FreeText
        if (-not (Invoke-WithRetry { Move-Item -LiteralPath $file.FullName -Destination $dest -Force; return $true })) {
            Write-Warning "Failed to rename $($file.FullName)"
        }
        $i++
        Write-Host "Renamed $i of $count" -ForegroundColor Green
    }
}

function Remove-EmptyFolders {
    param([string]$RootDir)
    foreach ($dir in Get-ChildItem -Path $RootDir -Directory) {
        Remove-EmptyFolders $dir.FullName
        if (-not (Get-ChildItem -Path $dir.FullName)) {
            Invoke-WithRetry { Remove-Item -Path $dir.FullName -Force; return $true } | Out-Null
        }
    }
}

function Prepare-And-Rename {
    param([string]$RootDir, [string]$FreeText)
    if (-not (Test-Path $RootDir)) {
        New-Item -ItemType Directory -Path $RootDir | Out-Null
    }
    Write-Host "Removing junk files" -ForegroundColor Magenta
    Remove-JunkFiles $RootDir
    Write-Host "Unzipping non-CAD files" -ForegroundColor Magenta
    Unzip-NonCadFiles $RootDir
    Write-Host "Zipping CAD folders" -ForegroundColor Magenta
    Zip-CadFolders $RootDir
    Write-Host "Renaming files" -ForegroundColor Magenta
    Rename-Files -RootDir $RootDir -FreeText $FreeText
    Write-Host "Deleting empty folders" -ForegroundColor Magenta
    Remove-EmptyFolders $RootDir
    Write-Host "Process completed" -ForegroundColor Magenta
}

# Interactive execution
$root = Read-Host 'Enter the root directory path'
$freeText = Read-Host 'Enter free text for renamed files (optional)'
if ($freeText) { $freeText = (Get-CleanFileName $freeText) -replace '\s*-\s*$', '' }
Prepare-And-Rename -RootDir $root -FreeText $freeText

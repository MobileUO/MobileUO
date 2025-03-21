# This takes an input file of a list of absolute paths of files that we want to symlink to the external CUO git submodule

# Define the target base directory
$targetBase = "C:\mobileuo\Assets\Scripts\ClassicUO\src"

# Read the list of file paths from the text file
$filePaths = Get-Content -Path "symlinks1.0.0.0.txt"

# Change to the target base directory
Set-Location -Path $targetBase

foreach ($sourceFile in $filePaths) {
    # Ensure the source file path is inside the target base directory
    if ($sourceFile.StartsWith($targetBase)) {
        # Trim the target base directory from the source file path to get the relative path inside the target structure
        $relativeTargetPath = $sourceFile.Substring($targetBase.Length + 1)

        # Calculate the number of subdirectories to determine the correct relative path
        $subDirCount = ($relativeTargetPath -split '\\').Length + 3
        $relativePathPrefix = ("..\" * $subDirCount) + "external\ClassicUO\src"

        # Construct the relative path to the source file
        $relativePath = Join-Path -Path $relativePathPrefix -ChildPath $relativeTargetPath

        Write-Host "Source File: $sourceFile"
        Write-Host "Relative Target Path: $relativeTargetPath"
        Write-Host "Relative Path: $relativePath"

        # Define the target file path
        $targetFile = $relativeTargetPath

        # Delete the existing file if it exists
        if (Test-Path -Path $targetFile) {
            Write-Host "Deleting existing file: $targetFile"
            Remove-Item -Path $targetFile
        } else {
            Write-Host "No existing file to delete: $targetFile"
        }

        # Create the symlink
        Write-Host "Creating symlink: mklink $targetFile $relativePath"
        cmd /c mklink $targetFile $relativePath
    } else {
        Write-Host "Skipping file not in target base directory: $sourceFile"
    }
}

Set-Location -Path "C:\mobileuo\"
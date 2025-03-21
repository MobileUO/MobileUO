# This is for printing out all symlink files in the solution

# Define the output file to write the list of symlinks
$outputFile = "symlinks.txt"

# Clear the content of the output file if it exists
Clear-Content -Path $outputFile -ErrorAction SilentlyContinue

# Get all files in the current directory and its subdirectories
$files = Get-ChildItem -Recurse -File

# Filter the files to only include symlinks
$symlinks = $files | Where-Object { $_.Attributes -band [System.IO.FileAttributes]::ReparsePoint }

# Print the list of symlink files
foreach ($symlink in $symlinks) {
    Write-Output $symlink.FullName
}

# Write the list of symlink files to the output file
foreach ($symlink in $symlinks) {
    Add-Content -Path $outputFile -Value $symlink.FullName
}
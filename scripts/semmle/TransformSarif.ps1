param
(
    [Parameter(Mandatory=$false)][string]$inputSarifFile = "d:\dev\Sample\scripts\semmle\original.sarif",
    [Parameter(Mandatory=$false)][string]$localSourceLocation = "d:\dev\Sample\"
)

# Install Sarif tool
dotnet tool install --global Sarif.Multitool --version 2.1.24

# Setting up first before calling sarif tool
$currentDirectory = [System.IO.Path]::GetDirectoryName($inputSarifFile)
$readySarifFile = "$currentDirectory\ready.sarif"
Copy-Item $inputSarifFile $readySarifFile

# Construct URI based on source server, GitHub or AzureDevOps
# Note: This assumes the location of source is associated with a git repo
$remoteOrigin = [System.Uri](git config --get remote.origin.url);
$commitId = git rev-parse HEAD
$repoUri = $remoteOrigin.AbsoluteUri.Replace('.git', '')
if ($remoteOrigin.Host -eq "github.com")
{
    $commitUri = $repoUri + "/blob/" + $commitId
}
elseif ($remoteOrigin.Host -like "*visualstudio.com")
{
    $commitUri = $repoUri + "?version=GC" + $commitId
}

# Insert snippets by pointing to location that has the source code
sarif rewrite $readySarifFile --insert "RegionSnippets;ContextRegionSnippets" --uriBaseIds "%SRCROOT%=$localSourceLocation" --pretty-print --force --inline

# Rebase source location to a repository location that has this commit change
sarif rebaseuri $readySarifFile --base-path-value $commitUri --base-path-token "%SRCROOT%" --force --pretty-print --inline

# Convert relative paths to full URI paths
sarif absoluteuri $readySarifFile --uriBaseIds "%SRCROOT%=$commitUri" --force --pretty-print --inline
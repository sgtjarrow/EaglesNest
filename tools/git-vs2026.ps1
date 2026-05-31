param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $GitArgs
)

$gitPath = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"

if (-not (Test-Path -LiteralPath $gitPath)) {
    throw "Visual Studio 2026 bundled Git was not found at: $gitPath"
}

& $gitPath @GitArgs
exit $LASTEXITCODE

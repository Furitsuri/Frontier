param(
    [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
    [string[]] $Files
)

$utf8Bom = New-Object System.Text.UTF8Encoding $true

foreach ($f in $Files) {
    $content = [System.IO.File]::ReadAllText($f)
    $converted = $content -replace "(?<!\r)\n", "`r`n"
    [System.IO.File]::WriteAllText($f, $converted, $utf8Bom)
    Write-Output "Converted: $f"
}

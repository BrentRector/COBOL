# LfText.psm1 — THE ONE WRITER of a tracked text file a PowerShell generator renders (kb/Work PB3029: one line ending,
# LF, in every tracked text file). `Set-Content` and `Out-File` end lines with the PLATFORM newline, and a
# `StringBuilder.AppendLine` writes `Environment.NewLine`, so on Windows every generator wrote CRLF; git stored LF, but
# the checkout kept the CRLF bytes until the next checkout, and `scripts/line_endings.py --check` (a gate audit) refuses
# a CR in a tracked text file's checkout. Writing LF over a CRLF checkout changes the file's SIZE, which git's index
# trusts over content, so `git status` then lists it modified over an empty diff; `line_endings.py --fix` re-states every
# such entry (refresh_index), so this writer stays a plain write. Import it and write through it:
#     Import-Module (Join-Path $PSScriptRoot 'LfText.psm1') -Force
#     Set-LfContent -LiteralPath $out -Value $sb.ToString()
Set-StrictMode -Version Latest

function Set-LfContent {
    <#
    .SYNOPSIS
    Write -Value to -LiteralPath as UTF-8 (no BOM) with every line ended by LF and one final LF — what
    `Set-Content -Value <string>` writes on Linux, on every OS.
    #>
    param(
        [Parameter(Mandatory)][string]$LiteralPath,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Value
    )
    $text = ($Value -replace "`r`n", "`n") + "`n"
    [System.IO.File]::WriteAllText($LiteralPath, $text, [System.Text.UTF8Encoding]::new($false))
}

Export-ModuleMember -Function Set-LfContent

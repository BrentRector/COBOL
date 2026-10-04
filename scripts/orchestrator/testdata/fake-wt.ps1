# A fake wt.exe for test_orchestrate.ps1: records each invocation's arguments, one line per call, in FAKE_WT_MARK.
Add-Content -Path $env:FAKE_WT_MARK -Value ($args -join ' | ') -Encoding utf8
exit 0

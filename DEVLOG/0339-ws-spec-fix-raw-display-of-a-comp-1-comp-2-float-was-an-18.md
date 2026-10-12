## Entry 339 — WS-SPEC fix: raw DISPLAY of a COMP-1/COMP-2 float (was an 18-digit integer)

`GetDisplayString` routed a COMP-1/COMP-2 numeric field through `FormatNumericForDisplay` with the synthetic
float descriptor's `TotalDigits=18`/`FractionDigits=0`, so `DISPLAY` of a COMP-2 holding 6.28318 printed
`000000000000000006`. Added a COMP-1/COMP-2 branch + `FormatFloatForDisplay` that reads the raw IEEE
float/double (`BitConverter.ToSingle`/`ToDouble` — the field bytes are little-endian IEEE, per DecodeComp1/2) and
renders the **shortest round-trip** decimal (an integral value has no point). CLI-verified: `6.28318`, `300`,
`-0.625`, `0`. Test: `SpecFixTests.Display_OfFloatItem_ShowsNaturalMagnitude`. 9th backlog fix — the COMP-1/2
cluster (arithmetic + display) is now complete; only the COMP-4 token remains in CompFloat.


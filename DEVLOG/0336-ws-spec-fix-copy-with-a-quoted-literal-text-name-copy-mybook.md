## Entry 336 — WS-SPEC fix: COPY with a quoted-literal text-name (`COPY "MYBOOK"`)

`CopyProcessor.ReadWord` consumed only letters/digits/`-`/`_`, so it stopped at the opening quote of a quoted
text-name and returned an empty library name → "copybook not found" (CBL3620). Added `ReadTextNameOrLiteral`
(strips the surrounding quotes, collapses a doubled embedded quote) and used it for both the COPY text-name and
the `IN`/`OF` library-name (ISO §7.2.3 — the `literal-1`/`literal-2` alternative). CLI-verified: `COPY "MYBOOK"`
resolves `MYBOOK.cpy` → `HI THERE`. Test: `SpecFixTests.Copy_QuotedLiteralTextName_Resolves`. 6th backlog fix.


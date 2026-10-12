## Entry 901 — 2026-07-18 21:10 PDT — PHASE-13 Wave G — EXCEPTION-FILE(connector) optional-arg form (VCR 68/69)

Landed the COBOL-2023 file-connector-argument form of `FUNCTION EXCEPTION-FILE` (§15.28.4 r2; Annex E.3.3 item 25) and
its national twin `EXCEPTION-FILE-N` (§15.29.4 r2; E.3.3 item 26). Unlike the no-argument form (which reports the LAST
exception), the arg form reports the **NAMED** connector's current I-O status + SELECT-spelled file-name (r2b), or two
alphanumeric spaces when the connector was never opened/attempted/accessed (r2a). This closes the last Wave G CLASS-A
implementation item (VCR 34 remains deferred).

**Design — a NEW binder path (the crux).** The argument is an FD file-connector-name, NOT a data reference — the
previous 1-arg form just rendered loud. `IntrinsicBinder.BindExceptionFileArg` (intercepting before the generic
`BindIntrinsicArgs`, the SUBSTITUTE/CONVERT pattern) resolves the argument text to its `FileModel` from
`ctx.Data.Files` (§15.28.3 rule 1; a non-file → **COBOLNET1574**) and carries it on a new `BoundIntrinsicCall.FileArg`.
The renderer (`IntrinsicRenderer`) then passes `EmitText.FileKeyExpr(fa)` — the SAME connector key every file verb uses
— to `RuntimeApi.EcFn("File"/"FileN", key)`, so dispatch/display share one key. Introduction-gated via the ONE
`ConstructRegistry` (new `exception-file-argument-2023` / `exception-file-n-argument-2023` rows → COBOLNET0900 < 2023).

**Runtime.** `EcFunctions.File(string key)` / `FileN(string key)` overloads → `CobolFile.ExceptionFile` →
`FileRegistry.ExceptionFile`: "  " if the connector was never accessed (a new **`FileConnector.EverAccessed`** flag set
by `Open` [successful OR attempted] + `FileRegistry.DeleteFile`) or unknown; else `Status` + the `::`-stripped display
name (the SAME emit-namespace strip as the no-argument `File()`). The `-N` twin reuses the ONE `NationalOf` repertoire
translation over the alpha rendering.

**Verified** all cases CLI-probed: opened INF read to EOF → `EXCEPTION-FILE(INF)` = `10INF`; a never-opened SELECTed
connector → `"  "`; `EXCEPTION-FILE-N` national `35INF`; a non-file argument → COBOLNET1574; `--std 2014` → COBOLNET0900.
Golden `tests/conformance/2023/exception_file_arg` (GreenfieldOnly — a greenfield EC + FileRegistry feature the frozen
legacy has no EC model for) + the two matrix rows. Registered COBOLNET1574 in DiagnosticCatalog (DIAGNOSTICS.md
regenerated). Gated with the FULL Conformance project (the `04c32a93`/`cf1fcaa2` lesson).


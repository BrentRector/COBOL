## Entry 2074 — 2026-10-16 08:01 PDT — PB934 retired: the per-name exception drift table it asked for landed as PB3102's registry

Wave 1078, group H, was dispatched PB934 (a catalogued EC- exception-name with no raise site and no EcEmitter gate row
compiles clean and wires nothing; owed: a per-name drift table requiring a raise site plus a gate row, or a recorded
determination). The re-probe on this tree's own build (0 warnings, 0 errors, main c29af1704) found nothing left to build.
PB3102 (wave 1070, group E) landed `EcRaiseSiteRegistryDriftTests` over the closed `ExceptionCatalog.AllLevel3` universe:
every level-3 name is declared by the binder (`EcBinder.AllDeclaredNames`) and raised by the runtime or code generator, or is
on `DeclinedRaiseSites` with a CONFORMANCE.md anchor the second test reads and requires to quote the name. The three failure
shapes (raised but never declared, declared but never raised, neither) each fail with their own message. All three tests pass
on this build. `EC-FLOW-GLOBAL-EXIT`, the note's own example of a name no rule sets, is declined under CONFORMANCE.md section 3
D-GLOBAL-EXIT.

One difference from the note's wording: the determination is stored in CONFORMANCE.md with the test table pointing at it, not on
the catalogue row, because the catalogue lives in the Runtime assembly and the anchor text is what a reader consults (design
D13). The note is retired (superseded), claims and closes no inventory row, and no source changed. The gate verdict line is
recorded in the wave report.


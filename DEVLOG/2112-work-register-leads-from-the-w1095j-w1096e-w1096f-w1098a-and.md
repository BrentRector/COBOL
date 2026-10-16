## Entry 2112 — 2026-10-16 14:45 PDT — Work register: leads from the w1095j, w1096e, w1096f, w1098a and w1098b reports filed (PB3292-PB3295; sections on PB3229 and PB3272)

PB3292: source-text lexical predicates still spelled outside `CobolSourceText` (three literal scanners and the `>>` directive indicator written three times); from the PB2621 report. PB3293: `CldrTailoringBuilder.Lookup` reads reset, extension and prefix strings by whole-key lookup instead of the S2.1 walk over NFD, suspected wrong collation order, unprobed against ICU; from the PB2762 report. PB3294: Windows console device decoding in `StandardStreams.EnsureUtf8` (interactive ACCEPT decoding, console code page leak), both unprobed; from the PB3193 report. PB3295: `EcRaiseSiteRegistryDriftTests` reads note status by substring; from the PB3196 report.

Sections added to existing open notes: PB3229 (the category spelling at `OperandClassScreen.cs:188` and `SetBinder.cs:911`, from the PB1400 report) and PB3272 (the PB2099 branch keeps `RunMain`). The other two PB2099 carried leads were already PB3270 and PB3271. Ids PB3296-PB3301 are unused.


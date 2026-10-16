## PB298: every OPTIONS model field has a consumer, and a drift test keeps it so

kb/Work PB298 (wave 1097 G) listed five `OptionsModel` fields as possibly set by `OptionsBinder` and read by nobody,
the shape PB152 had proved to be a silent wrong answer (an OPTIONS clause the program wrote, accepted in silence,
changing nothing). The note itself asked for a consumer search per field before calling any of them defective.

**Re-probe on the branch's own build, field by field.** `IntermediateRounding` is consumed by
`NumericRenderer.IntermediateMode` on every CobolDec operation: under `ARITHMETIC IS STANDARD-DECIMAL`,
`COMPUTE X = 2 / 3 * 3` into `9V9(5)` prints `199999` with `INTERMEDIATE ROUNDING IS TRUNCATION` and `200000` with
NEAREST-AWAY-FROM-ZERO, as §11.9.11.2 GR3 e) and b) require. `FloatBinaryEndianness` is consumed by `DataBinder` →
`PicInfo.FloatItem`: `FLOAT-BINARY DEFAULT IS HIGH-ORDER-RIGHT` gives a `FLOAT-BINARY-32` of 1 the image
`X"0000803F"` while an item with its own HIGH-ORDER-LEFT phrase keeps `X"3F800000"` (§11.9.8.3 SR2/SR3), and the
mirror probe gives the mirror answer. The two FLOAT-DECIMAL fields were already deleted by wave 59, which declined the
clause with COBOLNET2424. That left one producer-only field, `EntryConvention`. No program could observe it, because
the §11.9.7.3 SR1 / §11.9.7.4 GR3 screen admits only COBOL, which is also the implied convention (GR4 c), but it was
state built for nobody.

**Fix.** `OptionsModel.EntryConvention` is deleted and `OptionsBinder.EntryConventionOf` becomes
`ScreenEntryConvention`, the same checks (COBOLNET2385) with no return value. The class is closed by
`OptionsModelConsumerDriftTests` (Unit, tree sweep): it reflects every public property of `OptionsModel` and requires
a `…Options.<Field>` read in `src/` outside the producer and the declaration. It was run red first and named exactly
`EntryConvention`. A new OPTIONS field now fails until its consumer exists. `OptionsModel`'s class comment, which
still called FLOAT-BINARY and INITIALIZE capture-only, and the float-phrase comments that still named the declined
FLOAT-DECIMAL clause as a reader now describe the current code.

**Rows.** `GR-4.2.10-2` goes from PARTIAL to CONFORMS, which closes one GAP (72 → 71). The row's two NOT MET members
were the FLOAT-DECIMAL clause (now COBOLNET2424) and the EC-MCS-* exception-names (now COBOLNET1578, kb/Work PB374),
and both were re-probed. The seven ENTRY-CONVENTION rows' code-location moved to
`OptionsBinder.cs#ScreenEntryConvention`, retiring the old method and the deleted field as witnesses
(`batch-PB298.json`). `docs/CONFORMANCE.md` DRV-GR-11.9.7.4-4 and `DESIGN-external-repository.md` §3.4 name the
screen and say that the model has no field for the clause.

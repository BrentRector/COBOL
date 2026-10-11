## Entry 1381 — 2026-08-29 07:57 PDT — PB139 LANDED: ACCEPT whole — the receiver screens exist, the temporal store asks Table 16 and honours JUSTIFIED, and the device splits at the record boundary

Three mechanisms, one statement (kb/Work PB139). RECEIVER SCREENS: every §14.9.1.3 SR1/SR3 exclusion is
now a bind reject — class object, class pointer (both categories), a strongly-typed group and class index
share one screen (COBOLNET0818); a variable-length group draws the new COBOLNET1691 through the ONE
predicate (HasVariableLengthSubordinate); and an index-NAME receiver reports the R16 context diagnostic
COBOLNET1637 instead of the misleading UNDEFINED. The old code screened exactly one of the eight kinds and
its excuse comment ('await the PicCategory split') had been stale since PB124. TEMPORAL STORE: the MOVE-rules
legality is asked of the ONE Table 16 mechanism (MoveTable16.Refusal, integer-numeric sender) — the NATIONAL
receiver (Table-16 Yes, previously staged LOUD on legal source) binds and stores; the ALPHABETIC receiver
(Table-16 No, previously accepted SILENTLY) rejects — both automatic now, and the next category row is free.
JUSTIFIED is honoured through the new ONE alignment dispatch RuntimeApi.StrStoreAligned, extracted from the
six sites (MOVE ×3, STRING, INVOKE copy-back, ACCEPT) that each hand-rolled the same ternary. DEVICE: an
input line longer than 80 characters is CONSECUTIVE 80-character records, each padded (GR2 — the old
whole-line append delivered max(80, len) and CONTRADICTED the documented determination); a zero-length
receiver now CONSUMES its record and ignores every character of it (GR4b's closing sentence — the old
early-return handed record 1 to the NEXT statement, a wrong answer one line later); and the BOOLEAN device
receiver's conversion is DEFINED (AcceptSource.DeviceBoolean — '1' to one, everything else to zero, so the
D-B1 '0'/'1' invariant holds where the old default arm stored pad SPACES into boolean storage).

Verdicts: SR-14.9.1.3-1/-3 PARTIAL → CONFORMS, SR-14.9.1.3-6 NOT-IMPLEMENTED → CONFORMS, GR-14.9.1.4-4/-6
DIVERGES → CONFORMS, GR-14.9.1.4-1/-2 stay PARTIAL (the COMP-1/2 documented-loud deferral and the A.1
item-1/item-5 documentation rows — both PB160's half). COMP-1/2 receivers stay the pre-adjudicated
documented-loud deferral pinned by CheckOnlyCompileTests.


## Entry 912 — 2026-07-19 13:52 PDT — Review-fix: the VCR 16 STRENGTH half (§13.16.3 SR13 ¶2) — weak-TYPE external CONSTANT RECORD now rejected ≥2023

Review finding C9 (confirmed): the landed VCR 16 gate checked TYPE PRESENCE only; SR13 ¶2 requires the TYPE to
name a STRONGLY typed definition. The strength is known only at ExpandType (where the template resolves) — the
check lands there, beside the §13.18.22 SR5 external-type check: `IsConstantRecord && HasExternalClause &&
!template.TypedefStrong && DialectLevel >= 2023` → COBOLNET1549 (the same ConstantRecordRule descriptor as the
presence half). Scoped to the literal EXTERNAL-clause co-occurrence SR13 ¶2 names (the GR3
external-via-the-TYPE shape has no EXTERNAL clause; its strong external typedef is separately VCR-63-gated) —
the interpretation choice is documented at the check. Fixtures: negative
`external-constant-record-weak-type-at-2023` (COBOLNET1549 at 2023; CLI-probed) + the 2014 continuity golden
`external_constant_record_weak_type` (weak TYPE satisfies below 2023 — prints WXYZ; GreenfieldOnly, the legacy
suite diverges on the shape, verified by run). Also swept the stale "Annex E.3 item 10" labels in the two
sibling fixtures (E.2 item 10 — the Entry-910 correction propagated). Battery: legacy 166/166 · full greenfield
Conformance **3698** · (unit/characterization green earlier this tree). Wave I re-verify note: the finding's
EXCEPTION-FILE r2a / GOBACK RAISING / ref-mod-negative siblings remain queued in the spec-derive batch.


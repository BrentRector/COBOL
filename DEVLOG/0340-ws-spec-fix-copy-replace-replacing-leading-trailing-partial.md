## Entry 340 — WS-SPEC fix: COPY/REPLACE REPLACING LEADING/TRAILING partial-word substitution

`COPY … REPLACING LEADING/TRAILING ==partial== BY ==partial==` was unimplemented — the LEADING/TRAILING keyword
was mis-read as the from-operand, so no substitution happened. Added a `ReplaceKind` enum (Whole/Leading/Trailing)
to the replacement tuple (all 4 sites), detect the LEADING/TRAILING phrase in `ParseReplacements`, and match the
partial-word against the start/end of a single source text-word in `ApplyReplacements` (ISO §7.2.3.4 GR 9 b).
Whole-word replacement (the existing path) is the **default**, so existing COPY REPLACING / REPLACE behavior is
byte-unchanged (the SM suite stays green). CLI-verified: `LEADING ==PREFIX== BY ==XQ==` turns `PREFIX-FLD` into
`XQ-FLD`; `TRAILING ==-OLD== BY ==-NEW==` turns `ITEM-OLD` into `ITEM-NEW`. Test:
`SpecFixTests.Copy_ReplacingLeadingAndTrailing_PartialWord`. 10th backlog fix.


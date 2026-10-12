## Entry 1307 — 2026-08-18 05:50 PDT — PB88 lands: STRING / UNSTRING syntax-rule violations are bind-time rejections, through one helper

**PB88.** `STRING "12" DELIMITED SIZE INTO GP(3:3)` compiled clean and died at run time — the binder returned a
`BoundUnsupported` (the §1.4 loud stage meant for UNIMPLEMENTED legal constructs) for a whole family of syntax-rule
VIOLATIONS: STRING's reference-modified INTO (§14.9.43.3 SR4), edited INTO (SR5), a non-integer POINTER (SR7);
UNSTRING's non-alphanumeric/national sender (§14.9.48.3 SR2), DELIMITER IN / COUNT IN without DELIMITED BY (SR7),
non-integer COUNT IN / TALLYING / POINTER (SR5/SR6). Illegal source is a compile-time rejection in every COBOL, and
the wrong stage on illegal source is still the wrong stage.

- **One helper.** `StringUnstringBinder.Reject` reports **COBOLNET1651** and returns the stage as the backstop, so
  no rule site can forget the diagnostic half again; every SR site calls it. The family is complete: SR5's JUSTIFIED
  half, SR6 (a strongly-typed group INTO) and STRING SR11 / UNSTRING SR10 (a variable-length group operand —
  `ReferenceResolver.HasVariableLengthSubordinate`, the ONE §8.5.1.12 predicate written for PB70's ref-mod screen)
  were not checked at all. An UNRESOLVABLE name keeps the bare stage: the resolver already reported it. The USAGE
  rules (STRING SR1, UNSTRING SR4) keep their own COBOLNET1626.
- **Measured.** Eight negatives `pb88-string-into-{refmod,edited,strong-group,variable-length-group}`,
  `pb88-string-pointer-not-integer`, `pb88-unstring-{sender-numeric,count-without-delimited,pointer-not-integer}` —
  each COBOLNET1651 at every edition it applies to. Verdicts SR-14.9.43.3-{4,5,6,7,11} and SR-14.9.48.3-{2,5,6,7,10}
  → CONFORMS (GAP 4029 → 4019).

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|Negative|String|Unstring|SpecTraceability|Inspect` **741/741** · Unit
`SpecTraceabilityInventory|Registry|Diagnostic|Manifest|Guard|String` **153/153** · CLI probes. Docs:
COBOLNET_STRING_OPS_DESIGN.md (the hard-problems paragraph), DIAGNOSTICS.md, kb/Work PB88 → landed, plan §0.
Battery owed for the PB88 batch with the next landing.



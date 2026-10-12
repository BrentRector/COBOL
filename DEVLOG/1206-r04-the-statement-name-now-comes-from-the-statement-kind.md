## Entry 1206 — 2026-08-07 19:07 PDT — R04: the statement name now comes from the statement KIND — because `GO PARA.` never spells a `TO` for any token fix to find

**R04 is LANDED.** `FUNCTION EXCEPTION-STATEMENT` returned the statement's first TOKEN
(`EcBinder.cs` — `s.Start.Text.ToUpperInvariant()`), which is the §15.32.3 r2 name for 49 of Table 12's 50 rows
and `GO` for the fiftieth. One construction site, every layer downstream faithful; the binder was the whole fix.

⭐ **THE MEASUREMENT THAT SHAPED THE FIX: the token axis is wrong in a way no token can repair.**
`goToStatement : GO TO? …` — **TO is an optional word**, so `GO PARA.` is a GO TO statement whose tokens never
contain TO. That kills both plausible patches: the first-token special case returns `GO`, and a
longest-token-match against the name set — which looks structural — returns `GO` for the same program. §15.32.3
r3 names a property of the statement KIND ("the names of the statements are given in Table 12 … in the column
labeled 'Statement name'"), and the kind is exactly what the parse rule identifies.

**LANDED as `Table12StatementNames`:** the mechanical camelCase→spaced-uppercase projection of the parse-rule
name matches the Table 12 row for every regular rule — **`goTo` → `GO TO` falls out of the general mechanism
rather than being a special case**, and a newly added statement rule gets a correct name automatically (rule 5's
shape: the NEXT case is automatic). The exception map carries only rules whose NAME differs from their row:
`deleteFile` → DELETE (§14.9.10.1 — DELETE FILE is a format of the DELETE statement), `searchAll` → SEARCH
(§14.9.37 Format 2), and the five Wave-H facility arms whose affixes disambiguate GRAMMAR rules, not statements.
Rules with no 2023 Table 12 row at all (ALTER · ENTRY · ENTER · USE · NEXT SENTENCE · the 2023 inline method
invocation) keep their projected names as the DOCUMENTED answer — conforming source cannot observe them under
checking (>>TURN is 2002+), and each is adjudicated with its reason in the drift test, not silently defaulted.

**The drift test re-derives the whole mapping from the spec, BOTH directions**
(`Table12StatementNameDriftTests`): Table 12 scraped at its anchor (shape asserted first — 50 rows, ACCEPT…WRITE,
GO TO the only multi-word name), the grammar's `statement` alternatives scraped with comments stripped (each
asserted to be a SINGLE rule reference — the exact shape `NameOf`'s child-0 resolution depends on), every
alternative fed through the SAME `NameOfRule` path the compiler uses. Forward: every alternative resolves into
the table or the adjudicated exemption set. Backward: every table row is produced by some alternative — a row
with no rule is grammar or transcription drift. A hand-maintained resolver nothing re-derives would be exactly
the dead lookup `feedback_a_dead_lookup_is_also_unverified` warns about.

**Both spellings are pinned behaviorally** (`ExceptionConditionConformanceTests` ECT014/ECT015): a subscripted
DEPENDING operand raises EC-BOUND-SUBSCRIPT INSIDE the GO TO (§8.4.2.3.4's ambient wrap) under
`>>TURN … CHECKING ON WITH LOCATION`; the F3 declarative reads `STMT=[GO TO…]` and RESUMEs past. **Under the old
code BOTH facts print `GO`** — the first token is GO whether or not the source spells TO — so both discriminate
the fix, and ECT015 (`GO TGT-A TGT-B DEPENDING …`) is the spelling that defeats the token-derived alternatives.

**GATE (wave-local):** solution build clean · new drift tests 3/3 · EC conformance **56/56** (the two new facts
ran BY NAME: 2/2) · Unit **4044/4044**. Full battery deferred to the batch boundary per the tiered-gates
doctrine — no grammar change in this commit.


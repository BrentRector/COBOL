## Entry 1702 — 2026-09-25 01:54 PDT — Train 62: R43 derived selectors + finisher, SPECIAL-NAMES ordinals and bind order, variadic exact lists, EXP range (GAP 1334 → 1254)

Train 62 — three clusters in one landing (the orchestrator landed them now rather than holding the train to the
end of the day's quota): R43's derived selectors and their finisher (w59a + w59h, one cluster), the SPECIAL-NAMES
ordinal and clause-order fixes (w59k), and the variadic exact-list and EXP result-range fixes (w59l). One commit
per cluster, this entry and plan §0 in a final commit.

**Cluster 1 — R43 applied to clause rules (PB1198, PB1151, PB1255, PB1517, PB1518, PB579, PB1519, PB1564;
PB1536 Q1, PB468 Q9).** The owner's R43 ("unsupported means unsupported") says rules that exist only inside a
declined facility get DOCUMENTED-NON-SUPPORT. The derived-verdict engine now has four more axes, added to both
twins (`inventory_schema.py` DerivedSelector and `DerivedVerdictDriftTests.Select`): a cut that ignores NOTE
text, a `lead-in` axis (for the §9.3.6 exact-match list), a per-arm `excludes-pattern` (the old global
`interface-name` exclusion had silently dropped L5.2), and a conditionally-required axis. New selectors:
`mcs-only`, `float-formats-not-provided`, `record-key-source-only` and `a1-condition-absent`. Four existing
selectors gained arms. A fourth §8 derivation arm, `definitional`, closes §4.4's two conforming-run-unit rules.
Probing turned up three leaks, and all three are fixed. (1) `OPTIONS. FLOAT-DECIMAL IS BINARY-ENCODING.`
compiled with no diagnostic at 2014 and 2023, although Annex A.3 item 13 is declined. It now gets the §4.2.6
warning COBOLNET2424. (2) The endianness-first spelling was a parse error. The clause now reuses the USAGE
clause's single unordered `floatFormatPhrase` group, and the ordered copy is deleted. (3) An override whose base
formal is a variable-length group, overridden by a fixed group of the same width, crashed in Roslyn with CS0115.
The pair-mode `OoConformance.DescriptionMismatch` now refuses it with COBOLNET0829 (§9.3.5.3 item 7, §11.7.3
SR3). The unexplained `Generator_RunsClean` reds were a race: concurrent gates shared one fixed `%TEMP%`
directory, and 3 of 4 concurrent runs failed. Each run now gets its own GUID directory, in both generator scripts
(PB1564). The 14 audit_code_citations findings that main carried are repaired. The audit does not run in CI; the
w59h report files that as a lead. Goldens: 7 positive and 4 negative (w59_*, w59h_*). Rows: nine batches,
83 records, GAP 1334 → 1256 on the merged tree. Merge conflicts: `CONFORMANCE.md` DOC-A.1-143 takes this
cluster's witness and keeps main's DOC-A.1-148 witness from golden batch 2; `DIAGNOSTICS.md` keeps both 2424
and main's 2427; three manifests take both lists of whole entries.

**Cluster 2 — PB1557 + PB1558 (w59k).** An ALPHABET that names all 65,536 native characters is legal (SR14 b4/c4
say "shall not exceed"), but its 16-bit position counter wrapped to 0. CHAR and ORD gave wrong answers, and CLASS
and SYMBOLIC … IN refused every ordinal. The counter is now `int`, and `CollatingTable.Build` checks its own
invariants. Sibling fix: an 11-digit literal-phrase ordinal used to be reported under the noninteger class rule.
It is now read from the parse by one saturating reader. PB1558: ALPHABET … IS LOCALE was resolved in source
order, although the §12.3.7.2 format puts LOCALE after it. SPECIAL-NAMES now binds in dependency order. Eight
goldens were written with LOCALE before ALPHABET, which the format does not allow; they now follow the format's
order. **Merge conflict:** train 60 (PB1058) had already switched SYMBOLIC CHARACTERS integer-1 to
`IntegerOperandRules.HostValue`, the one reader for integer-n values, while this cluster had put `OrdinalValue`
there. The merge keeps HostValue, which saturates the same way, and keeps the cluster's text for the SR16
message. `OrdinalValue` now serves only the literal phrases. The negative `pb1557-symbolic-ordinal-too-long`
passes by name. ⚠ Probe after the merge: that program gets TWO diagnostics, COBOLNET2427 (the implementation
limit, from `IntegerOperandPass`) and COBOLNET1670 (SR16 e2). Main already behaved this way before the train.
`IntegerOperandPass`'s own slot table says the ordinal's range is SR16's rule, reported by the binder, but
`BeyondHostLimit` ignores that classification. This is filed as a lead for the registrar. Rows: 5 witness
records, GAP 1256 → 1254 (+2, not +4: golden batch 2 had already closed two).

**Cluster 3 — PB1565 + PB1566 (w59l).** PB1041's exact argument handling now reaches the variadic functions.
VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE over an all-exact argument list evaluate their EAE with the one
Dec body and round only the result: VARIANCE(1E17+1 1E17+2) = 0.25, where it was 0. A drift test requires every
variadic float function to use this path. EXP and EXP10 results outside the binary64 range are now the §14.7.5
case-5 size error condition, through the one check `CobolIntrinsics.InBinary64Range` (EXP(1000) used to print
Infinity). DOC-A.1-179 gains its fifth checked place. PB1566's "SDIDI carrier" premise was re-derived and
rejected. Only `c8676ae78..65a174ed8` came in, because the branch's PB1041 merge was already on main. Rows: 6
witness records, GAP unchanged.

**The train.** The train was merged and gated on 332468ba4, then waited for train 61 (L3) and was rebased onto
de53a62b0. The rebase had conflicts only in the manifests, which took both lists of whole entries. The inventory
was reset to main's copy, and every cluster's verdict batches were re-applied in order with the same deltas. Since
there were conflicts outside docs/kb, the train was re-gated. Both gates ran the whole Conformance assembly,
unfiltered, plus full Unit, Characterization and the legacy Integration assembly, all `--no-build` after one
solution build at Normal priority. Pre-rebase: Conformance 8793/8793, Unit 29280, Characterization 33,
Integration 503 (+1 skipped). After the rebase: `Passed! - Failed: 0, Passed: 8838, Total: 8838` (Conformance,
9 m 15 s) · `Passed! - Failed: 0, Passed: 29298` (Unit) · `Passed! - Failed: 0, Passed: 33` (Characterization)
· `Passed! - Failed: 0, Passed: 503, Skipped: 1` (Integration). All 17 new goldens and negatives ran by name
(MANDATORY-PRACTICES I7): 22 cases, all passed. The very first build failed in `GenerateIfNewer.ps1`, the fresh
worktree's first ANTLR generation. Re-running it by hand succeeded, and every build after that was clean. semgrep
verify: PASS, no count changed. Main carried two audit_code_citations findings in golden-lane comments: a bare
"GR14" (REWRITE §14.9.35.4) on a line citing §9.1.14 in `85/l1c26_rewrite_relative_invalid_key` (batch 2), and
a bare "GR4" (TERMINATE §14.9.46.4) on a line citing §14.9.33.4 in `2002/l1c33_terminate_resume_next_implicit`
(batch 3). Both references now carry their full clause number, a comment-only change, so the CITATIONS leg is
GREEN as cluster 1 intended. Doc citations, evidence supersession, witness loss, Annex A.1 and `work.py check`
are all clean. **GAP 1334 → 1254 (−80).** No cluster was dropped. Work items landed: PB1198, PB1151, PB1255,
PB1517, PB1518, PB579, PB1519, PB1564, PB1557, PB1558, PB1565, PB1566.


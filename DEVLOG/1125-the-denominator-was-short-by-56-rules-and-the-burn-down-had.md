## Entry 1125 - 2026-07-30 02:30 PDT - The denominator was short by 56 rules, and the burn-down had been measuring against a total that flattered it

**GAP 3758 → 3814, and that is the number going the RIGHT way.** The P14 metric is `GAP / 3,790 normative rules`.
The 3,790 was wrong. The catalog now holds **3,846**, and every one of the 122 adjudicated rows carried forward
(zero removed), so nothing verified was lost — the denominator simply grew to what the standard actually contains.

**HOW IT SURFACED.** Adding the twelve batch-2 functions to PB1's `Verified` table, I went to read ASIN's argument
rule and the catalog had **none** — only RV and FMT. ACOS §15.8.3 has argument rules; ATAN §15.11.3 has them; ASIN
§15.10.3 had zero. The spec has them:

```
#### 15.10.3 Arguments rules
1) Argument-1 shall be of class numeric.
2) The value of argument-1 shall be greater than or equal to –1 and less than or equal to +1.
```

**"Argument*s* rules".** The extractor's `KINDS` map keys on literal heading spellings and carried
`"argument rules"` / `"argument rule"` — not the plural noun. Three clauses (§15.10.3, §15.13.3, §15.100.3) were
never recognised at all, and §15.69.4 says "Returned *values* rules".

⛔ **AND THE MAP'S OWN COMMENT SAID THIS HAD HAPPENED BEFORE**: *"A plural-only map skipped 15 whole blocks —
invisible to the empty-block check, because a block that is never RECOGNISED is never counted as seen."* The
previous fix was to add the singular spellings to the list. The list came back. Adding two more spellings would
have set up the third occurrence, so the pluralisation is **normalised away** — `canonical_title` singularises
`rules`, `arguments` and `values` independently, since the standard is inconsistent on all three axes
separately, and a map of literal combinations needs a new entry per combination forever.

**THE GUARD IS THE REAL DELIVERABLE.** A *parse gap* is a block that was recognised and yielded nothing; a block
whose heading spelling is unknown is never recognised, so it is never a gap, never counted, never missed — the
denominator is just quietly short. The extractor now walks every numbered heading whose title contains "rule" and
reports the ones `KINDS` cannot resolve. It fired immediately on four more, and **three remain reported**:
§13.18.40.5 "Editing rules", §13.18.40.6 "Precedence rules" and §5.3 "Rules". Those are NOT spelling variants —
they are rule KINDS the catalog has never modelled, and adding a kind changes what the denominator means. Left
loud for an owner decision rather than absorbed on my judgement.

**⚠ THE OTHER 43 ARE STALENESS, NOT SPELLING.** Ten rules come from the heading fix. The rest arrived because the
catalog had been frozen since before the de-paging: the extractor HALTED on a missing page anchor and had not run
since, while the transcription gained repaired rules from the figure sweep, the table merges and the run-in format
repairs. **A denominator that cannot be regenerated silently stops tracking its source.**

**THE HALT IS DISCHARGED BY DELETING WHAT IT GUARDED.** It existed because the builder stamped a PAGE on every
rule and would have recorded zero for all of them after de-paging. The column is gone: a rule is cited by its
clause — 14.9.41.2 GR3, never "page 754" — so the field was dead weight even while pages existed
([[project_no_page_numbers_in_markdown]]). Removing it re-keys the catalog onto the clause hierarchy, which is
what plan §0 had listed as the blocker.

**GATES.** `SpecTraceabilityInventoryDriftTests` 10/10 over 3,846 rows. Unit **963/963**. Characterization
**33/33**. 122 adjudicated rows preserved, 56 new, 0 removed.


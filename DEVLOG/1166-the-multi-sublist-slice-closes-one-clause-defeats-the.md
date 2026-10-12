## Entry 1166 — 2026-08-04 02:05 PDT — The multi-sublist slice closes, one clause defeats the manifest's shape, and admitting a ninth exposed 326 wrong rule-ids

**Eight more clauses admitted, and two things came out of the work that are worth more than the eight.**

**THE ADMISSIONS.** §9.1.14 · §9.3.6 · §9.3.8.2.3 · §14.6.2.3.2 · §14.6.13.1.5 · §14.8.2.3.2 · §14.8.2.3.3 ·
§14.8.3.3, all **GR**, each grounded on §5.3.3 ("a general rule defines or clarifies the meaning or relationship
of meanings") and explicitly NOT SR, because §5.3.2 scopes syntax rules to *"the order in which words or elements
may be WRITTEN"* and these state execution semantics. Both citations `cite.py --check` validated.
**Denominator 3,904 → 3,981**, 421/421 verdicts carried forward, GAP 3,830 → 3,907. Fifteen clauses / 120 rules
admitted; 85 clauses / 344 ordinals still pending.

**⚠ §8.3.2.2 DEFEATED THE MANIFEST, AND THE REASON IS A MECHANISM LIMIT RATHER THAN DOUBT.** All five of its
sub-lists are normative, but they are **not all the same kind**. *"A given user-defined word may be used as only
one type … with the following exceptions"* and *"Externalized names shall be referenced in a source element
only:"* constrain where words may be **written** — syntax rules. *"The following user-defined words shall be
externalized to the operating environment"* and the INVOKE/CALL naming-convention lists state **effect** —
general rules. The manifest carries ONE `kind` per CLAUSE, so admitting it either way would mis-type half its
rules. It needs **per-SUBLIST kinds**, which is a manifest and extractor change, not a judgement call. It is the
first of the 100 to need one and it will not be the last. Recorded in the file with that reasoning rather than
skipped quietly.

**⛔ AND ADMITTING §9.3.8.2.3 EXPOSED PB39 — 326 RULE-IDS THAT DO NOT MATCH THE STANDARD'S OWN NUMBERING.**
The manifest predicted 8 rules; the extractor harvested 12. The clause's raw ordinal sequence is
`1,2,3,4,5, 1,2,3, 6,7,8,9` — **nine top-level rules with a three-item NESTED list inside rule 5's item c)**,
which the transcription flattens onto column 0. `extract_rule_catalog.py` opens a sub-list on the backwards jump
(correct) and then **never returns to the parent**, so top-level rules 6–9 are filed as
`GR-9.3.8.2.3-L2.6 … -L2.9`.

It is **pre-existing and widespread — 40 sub-lists, 326 rules, 8 % of the catalog.** The signature is a sub-list
whose ordinals are not contiguous-from-1; the worst is `SR §12.3.7.3 L7: [1,2,3,4,5, 18…32]`, fifteen top-level
syntax rules mis-filed.
⚠ **The COUNT is unaffected, so the denominator is sound** — every rule is harvested exactly once and 3,981 is
right. What is wrong is the rule's **identity**: `SR-12.3.7.3-L7.18` *is* "§12.3.7.3 syntax rule 18", and nobody
reading the standard would guess that id. For a project whose deliverable is spec↔code traceability, that is the
defect, not a cosmetic detail.
⭐ **Exposure measured BEFORE deferring it**, because "I'll fix it later" is only honest with a number attached:
**exactly two adjudicated rows** use a mis-segmented id — `AR-15.68.3-L3.6` and `AR-15.68.3-L3.7`, which are
§15.68.3 rules 6 and 7, *the NUMVAL-C digit caps PB33/PB34 landed the same day*. Everything else is an untouched
GAP row the next `build_inventory` would simply re-key. Filed as PB39 with the parent-stack fix and the migration
note, rather than attempted at the end of a long session.

**GATE:** extractor `--check` clean (zero parse gaps) · `build_inventory` carried 421/421 ·
SpecTraceabilityInventory 10/10.


# Learnings ledger

**Owner, 2026-09-28:** "We should persistently track what devlog entries we've processed into tangible vetted
learnings and incorporated into skills. It would be a massive waste to do all this evaluation over the entire devlog
again in a month just to capture new learnings."

This directory makes turning the devlog into learnings INCREMENTAL. Each consolidation reads only the entries after
the watermark, and a candidate that has been decided is never re-validated from scratch. The pipeline itself (devlog
→ candidates → adversarial validation → vetted learnings → skills) is `kb/Work/PB1711` and the public devlog skill's
`templates/consolidate-brief.md` and `validate-brief.md`.

**Append-only:** a record is never edited; a later run adds a new record. This is the frozen-evidence rule of the
parent directory.

## Files

- **`watermark.json`**, one object per consolidation run, appended:
  `{"run", "date", "consolidated_through_entry", "entries_read": [first, last], "candidates": N, "note"}`.
  The next run reads DEVLOG entries with numbers ABOVE the latest `consolidated_through_entry`.
- **`candidates.jsonl`**, one line per candidate:
  `{"id": "C<n>", "run", "theme", "title", "text", "source_entries": [DEVLOG entry numbers], "sources": [...]}`.
  The ids are stable and never reused. Run `consolidation-1` wrote its lines without `text`: its candidates' bodies
  (problem, root cause, fix, evidence) are in `consolidation-1-candidates.md`, matched by title. Steps 3 and 6 below
  read them there.
- **`consolidation-1-candidates.md`**, the full text of run `consolidation-1`'s 145 candidates, as drafted (the
  withdrawn LEARNINGS.md that `watermark.json` names). It is the only copy of those bodies, so it is kept.
- **`verdicts.jsonl`**, one line per decision:
  `{"id", "run", "date", "verdict": "VETTED|UNPROVEN|REFUTED", "validator": {...}, "refuter": {...}, "evidence",
  "missing", "contradiction", "correction"}`.
  - The latest line for an id is its current state.
  - An UNPROVEN candidate may be re-decided later, when the missing evidence exists, by appending a new line.
  - A REFUTED one stays refuted unless new evidence reverses the refutation; that is also a new line, with the
    source.
- **`encoded.jsonl`**, one line per incorporation into a skill:
  `{"id", "date", "skill", "section", "version", "commit"}`.
  Only VETTED ids may appear here (owner, 2026-09-28: only vetted learnings are encoded into skills).

## The next run (the incremental procedure)

1. Read the latest `consolidated_through_entry`, W.
2. Consolidate ONLY DEVLOG entries numbered above W into new candidates, each with its `source_entries`.
3. Before validating, drop or merge any new candidate that restates an existing id. Match on the title and the
   mechanism; record it as a merge, not as a new id.
4. Validate the new candidates, and append their verdicts.
5. **Re-check:** for every currently VETTED id, search ONLY the new entries for anything that contradicts it. A
   contradiction appends a new verdict line for that id (UNPROVEN or REFUTED) and triggers removal from the skills.
   This is the one piece of old work each run repeats, and it is a targeted search, not a re-validation.
6. Re-decide any UNPROVEN id whose missing evidence the new entries supply.
7. Append the new watermark.

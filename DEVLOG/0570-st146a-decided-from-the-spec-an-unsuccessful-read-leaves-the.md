## Entry 570 — 2026-06-10 22:25 PDT — ST146A decided from the spec: an unsuccessful READ leaves the record area UNCHANGED (the canonical refinement of §14.9.30 GR18) — re-baselined + locked (827 conformance; 273 NIST)

**The owner asked for a definitive spec search on ST146A. The search settled it — and the divergence was NOT
the "RETURN INTO tail" the old note guessed.** The true mechanism, derived from the source: ST146A's X-card
FILE-DUMP section reads SQ-FS1 while it is CLOSED — the SORT input procedure closes it (S-I-P-3) and F-D-1
never re-opens it (a CCVS bug: F-D-3 opens SQ-FS2 for the second dump, so the missing OPEN is an oversight,
and NIST never saw the dump it intended on ANY implementation). Each of the 10 READs is unsuccessful with
status '47' (§9.1.13.7 item 7 / §14.9.30 GR2), the at-end family never arises so `AT END` correctly does not
fire, and the program then prints the record area. **What the spec says about that area: §14.9.30 GR18 —
"Unless otherwise specified, at the completion of any unsuccessful execution of a READ statement, the content
of the associated record area is UNDEFINED" — and the spec's own undefined-elements catalog (Annex item 40)
lists exactly this case.** Both outputs conform; the spec canonizes neither. The single diff hunk is exactly
the 20 dump lines (legacy: ALL LOW-VALUES; greenfield: the area's stale last content, record #1000's image);
every test verdict and the SQ-FS2 dump match byte-for-byte.

**The canonical decision (owner-directed "decide definitively"):** COBOL.NET's behavior — **the record area is
UNCHANGED after an unsuccessful READ** — because (a) it extends the spec's OWN pattern for every other
unsuccessful I-O verb, which it does specify: REWRITE GR14, WRITE GR15, DELETE GR8, START GR2 all say
"unaffected/does not alter"; READ's "undefined" is the lone exception and GR18's "unless otherwise specified"
licenses the refinement; (b) the legacy's LOW-VALUE fill is a byte-engine artifact of the zeroed
`ProgramState` — the substrate the pivot abolished; (c) it is deterministic. Documented at `EmitRead` (the
store sits in the success branch by construction) and PINNED by the new
`ReadNotOpen_Status47_AtEndNotTaken_RecordAreaUnchanged` spec test ('47' + no AT END + area intact).

**Owner challenged "NIST would have fixed it if unintentional" — the corpus itself settles intent:** ST115A
(same suite, same 4.2 release) carries the IDENTICAL column-7-X dump idiom and DOES open the file first
(`XFILE-1-DUMP-INIT. / X OPEN INPUT SQ-FS1.` before its read loop), as does ST146A's own second half (F-D-3
opens SQ-FS2). ST146A's F-D-1 deviates from the suite's own established idiom — an omission, not a design. Why
it survived: the X-population is the CCVS's site-optional DEBUG aid — free-form dump text with no PASS/FAIL
verdicts, never part of the scored validation summary (the verdicts, which WERE scored, are identical across
both compilers here) — and CCVS85 4.2 (Apr 1993, per every program's header) was the FINAL release before
NIST wound the COBOL validation program down, so no maintenance pipeline ever looked again. Even had they
looked, the dump's content is spec-undefined ('47' + GR18) — there was no "right" output to score against.

**Mechanics:** ST146A's golden re-baselined from the verified greenfield run (zero FAIL*, 004 OF 004; the one
hunk is the dump) and locked as a differential row (**273 NIST**). The guard list is renamed
`LEGACY_NONCONFORMANT` → **`LEGACY_DIVERGENT`** — more accurate now that it carries two classes, each
program tagged in `scripts/guard.sh`: HOLE (a verified legacy non-conformance) vs UNDEFINED-CHOICE (a
different implementor choice of spec-undefined behavior — ST146A). Full legacy guard: **356 MATCH + 8 LEGACY
DIVERGENT, 0 regressions, ALL GREEN** (1204 unit + 536 integration). Also fixed the sweep's binary
false-green (`diff -a` — a golden carrying binary bytes made plain diff print "Binary files differ" with no
countable rows, which read as GREEN; that is how pre-570 ST146A hid in the census). **827 conformance + 15
unit green.**


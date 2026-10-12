## Entry 1191c — 2026-08-06 15:16 PDT — PB33: NUMVAL-C enforces §15.68.3 r4a through its VALIDATING TWIN, and the defect was a wrong answer rather than the under-rejection the note claimed

> ⚙ **RECONSTRUCTED 2026-08-07 from the primary sources — the commit message (`bd778375`), its `kb/Work/` note and its diff — not written at the time.** The commit landed without a DEVLOG entry; the owner's call was to reconstruct where it can be done accurately. Every fact below is carried from those sources; nothing is recalled. **Lettered rather than renumbered** because entries 1192–1198 are already referenced by pushed commit messages, which cannot be edited.

§15.68.3 r4a: "Argument-1 shall have one of the following two formats." **TEST-NUMVAL-C (§15.94) implements both
exactly — that is its entire job — while NUMVAL-C stripped the currency and grouping separators, delegated to
NUMVAL, and checked nothing.** The note called this "the validating twin was fixed, the value-producing one was
not", which is right, and filed it `under_rejects`, which was too kind:

```
NUMVAL-C("12$34")     → 1234        A WRONG ANSWER   (TEST-NUMVAL-C: error at position 3)
NUMVAL-C("$1,2X4.5")  → 0, silently                  (TEST-NUMVAL-C: error at position 5)
NUMVAL-C("1.2.3")     → 0, silently                  (TEST-NUMVAL-C: error at position 4)
```

Interior currency is neither r4a format, and stripping it turned malformed input into a plausible number. **The
note's `wrong_answer` and `silent` flags are now set — it had ranked itself below what it actually did.**

**THE FIX IS THE VALIDATOR, NOT A SECOND COPY.** `NumvalC` now calls `TestNumvalC` and raises
EC-ARGUMENT-FUNCTION on a non-zero verdict (§15.3 — "If the evaluation of an argument results in an incorrect
value for that argument … the EC-ARGUMENT-FUNCTION exception condition is set to exist"; with checking not
enabled the same clause leaves the result implementor-defined, which `ArgumentError`'s 0 return supplies). The
digit cap already worked this way (PB33's landed half — "the rule has ONE implementation for both functions
rather than a copy per twin"), so the whole rule now has one home and the two functions cannot disagree
(`feedback_one_rule_one_place`).

**VERIFIED, NOT ASSUMED:** with `>>TURN EC-ARGUMENT-FUNCTION CHECKING ON` the declarative is dispatched and the
run terminates (Table 13 fatal, §14.6.13.1.3 #7); with checking off the default stands. Conforming values are
untouched — `$1,234.5` → 1234.5, `$1,234.5CR` → −1234.5, `$12.34-` → −12.34, `$1,23,4.5` → 1234.5 (r4a's
grouping is `digit [, digit]…`, with no 3-digit constraint).

⚖ **EXTERNAL ORACLE:** NUMVAL-C is well within GnuCOBOL's corpus (unlike PB46's INVOKE), and the differential
reports 0 per-case flips across 1,323 programs — nothing that previously compiled changed verdict.

**GATE.** `=== BATTERY: ALL GREEN ===` — FULL greenfield Conformance **4223/4223** zero skipped · Unit
**3986/3986** · characterization **33/33** · guard-fast ALL GREEN with NIST 353 MATCH / 0 REGRESSION and NIST
AUDIT CLEAN · GnuCOBOL differential 1323 cases, **0 PER-CASE FLIPS**. Golden
`pb33_numval_c_format_validation` — 8 assertions, each nonconforming case PAIRED with TEST-NUMVAL-C so the two
functions are asserted to AGREE rather than merely both exist.


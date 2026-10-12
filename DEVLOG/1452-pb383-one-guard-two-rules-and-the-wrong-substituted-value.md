## Entry 1452 — 2026-09-04 15:45 PDT — PB383 — one guard, two rules, and the wrong substituted value for both

**PB383 — one guard, two rules, and the wrong answer for both.** `FUNCTION BOOLEAN-OF-INTEGER(5, 8192)` printed
`[0]` where the implementor's own documentation says `[]`. The condition was
`if (length < 1 || length > 8191) { …; return "0"; }` — a single arm holding **two different rules with different
owners**. `length < 1` violates §15.13.3 r2, an ARGUMENT rule ("Argument-2 shall be a positive nonzero integer");
`length > 8191` violates §15.4, the returned-value-LENGTH rule ("If the length of the returned value exceeds the
maximum length specified by the implementor for a returned value, an EC-ARGUMENT-FUNCTION exception condition is
set to exist"), and 8,192 *is* a positive nonzero integer so no argument rule is violated there at all. Both
documented answers are a **zero-length value** (rows `DOC-A.1-90` and `DOC-A.1-93`); the code answered the
one-position boolean `"0"` to both, silently, in a user's program.

The interesting part is why it drifted. Row 93's *other* enforcement site, `BaseConvert`, has always returned the
zero-length value — the same determination, two implementations, one wrong: the two-arm dispatch shape again. The
reason it could happen is that at every string-returning guard the **raise** and its **substituted value** were two
separate statements. The numeric half of that determination had lived in exactly one place since forever
(`ExceptionState.ArgumentError`'s own `return 0`); the text half had no home, so each guard wrote its own — and one
of them wrote a different answer. So the fix is not the condition split alone but
`ExceptionState.ArgumentErrorZeroLength`, the text twin: it raises *through* `ArgumentError` (one fatal path, one
checking gate) and returns `string.Empty`, making raise and substitute a single expression that cannot disagree.
Ten further guards moved onto it — BASECONVERT's five, the FORMATTED-* family's four, and §15.87.4 r1's SUBSTITUTE
arm, where the STANDARD rather than a determination states the zero length. The method's doc is explicit that it
does **not** decide the answer is zero length: the call site's citation does, and the two provenances are cited
separately. That is the rule-5 call — the brief said "match `BaseConvert`'s shape", and `BaseConvert`'s shape *was*
the two-statement raise-then-return that allowed the drift, so matching it literally would have reproduced the
hazard; the change introduces the mechanism `BaseConvert` should have been using and moves `BaseConvert` onto it.

The sweep found no second wrong answer. `CHAR`/`CHAR-NATIONAL`'s one SPACE is correct — row 90's
fixed-one-character clause — and after the change those four are the only literal-string substitutes left at any
`ArgumentError` guard in the runtime. The `LOCALE-DATE`/`LOCALE-TIME` family was deliberately left alone: §15.52.4
r3 makes its length depend on the *locale*, so it is in neither of row 90's classes and no row writes its answer
down — an open determination, now named in `docs/COBOLNET_INTRINSICS_DESIGN.md` rather than quietly absorbed, and
carried out of the item as a note-ready finding rather than fixed under a rule nobody has written.

`BOOLEAN-OF-INTEGER` ended up with **three** arms, and the third is the point: argument-1 negative with a *valid*
argument-2 keeps its determined length and returns argument-2 zero boolean positions, row 90's general clause — not
zero length. That arm is the discriminator, and it is a golden leg in both witnesses, because without it `OVER=[]`
could be read as "the function returns nothing whenever anything is wrong". Row 90 gained the BOOLEAN result class
it never had, and its zero-length clause is now stated as a rule rather than a closed list.

Two goldens landed in `tests/conformance/2023/`. Writing them turned up a trap worth recording: the obvious
receiver for a boolean result is `PIC 1(n) USAGE BIT`, and §14.6.8.6 **zero-fills** a boolean receiver — so the
defective `"0"` and the correct zero-length value both arrive as `00000000`, and the natural golden would have
passed on the bug. Each leg therefore pairs a DISPLAY (§14.9.11.4 GR1 — a zero-length operand transfers nothing)
with a `FUNCTION LENGTH` read-out (§15.50.4 r1 — the length as a number).
`l1_returned_value_limit_boolean_repro` also puts 8,191 *inside* the bound, pinning `> 8191` rather than
`>= 8191`; `l1_argument_default_boolean_length` is the first witness the row-90 BOOLEAN-OF-INTEGER site has ever
had, and every one of its arguments is a data item so the runtime guard — not a literal fold or the bind-time class
screen — is what is under test. Both bind-time touchpoints were probed rather than reasoned about
(`IntrinsicArgumentRules`'s `Uniform('i', …)` screens the class half only;
`ConditionBinder.StaticBoolCallWidth` computes a receiver width a zero-length sender then zero-fills), and
`BOOLEAN-OF-INTEGER` is `IntrinsicBind.Runtime` — there is no compile-time twin to disagree.

Rows re-verdicted: `DOC-A.1-90` re-recorded with the new witness and the boolean class named, `DOC-A.1-93` closed
(it was `GAP` purely for want of a witness). The row's close measured **3054 → 3053** against the pin it was cut
from; the lane-3 batch-2 registration (Entry 1451) landed 3054 → 3022 in between, so on main it reads
**GAP 3022 → 3021** — the registrar touched no `DOC-A.1-9x` row, and the rebase merged the two inventory edits
without a conflict. The implementer made the gate fail once before
trusting it — reverting `OVER=[]` to the defective `OVER=[0]` produced `Failed: 1`, restoring it produced
`Passed! 1324/1324` — and the landing re-ran the whole wave-local gate on its own build: conformance 1622/1622,
unit 5293/5295, characterization 33/33, `audit_code_citations` and `audit_doc_citations` 0 findings, semgrep
`verify.py` unchanged at the `PB175` baseline (36 / 439 — no increase). The two unit reds are
`ExternalCorpusPopulationDriftTests`, which print their own cause: the GPL GnuCOBOL corpus is absent from a fresh
worktree by construction and is never committed.

Three findings were carried out of the item for registration rather than folded into it: the `LOCALE-*` family's
unwritten substitute class; a disagreement between row `DOC-A.1-90`'s zero-length *clause* and its own example list
for the FORMATTED-* family (documentation precision only — the behavior is witnessed green); and `FUNCTION LENGTH`
accepting a function-identifier argument, which is correct but takes a three-clause derivation (§15.50.3 r1 +
§8.4.3.2.1 + §8.4.3.2.4 r2) that a future reader could mistake for a leniency.


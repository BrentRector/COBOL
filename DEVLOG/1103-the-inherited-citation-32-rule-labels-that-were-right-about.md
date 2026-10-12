## Entry 1103 — 2026-07-29 11:35 PDT — The inherited citation: 32 rule labels that were right about everything except the rule

Rule 1 says a citation you did not run `--check` on is not a citation, and that the failure mode is not
inventing one but INHERITING one. This entry is that rule catching something, so it is worth writing down
precisely what it caught, because the defect is subtler than "wrong number".

Validating the V59 documentation's clauses turned up a failure on `§15.14.4`. Everything about the citation was
right except one word. The clause number resolves. The quoted text is genuinely in the standard. The rule number
is correct — §15.14.4 rule 1 really does say "the returned value is an integer that is the length of argument-1
in number of bytes". What is wrong is the LABEL: §15.14.4 is titled **"Returned value rules"**, not "General
rules", so writing `GR1` names a rule category the clause does not contain.

What makes this the inherited-citation pattern rather than a typo is why it looked right. The V59 work sits
almost entirely in §13.18.60.4 and §14.9.30.4, and BOTH of those genuinely are "General rules" — so `GR4`,
`GR11`, `GR14` around it are all correct. The two intrinsic clauses are the odd ones out, and `GR` was applied
to them by pattern-matching their neighbours. Nobody invented it; it propagated, from a design doc into code
comments, into a golden's header, into the fix queue, into the DEVLOG — and, this session, into two citations I
had just written in entry 1102 before the check ran.

The sweep: **32 occurrences across three clauses** — §15.14.4, §15.50.4 and §15.97.4, all "Returned value
rules" — in 12 files spanning `src/`, `tests/`, four design docs, the fix queue and this log. All are now the
`r<N>` form the intrinsic binder had been using correctly all along, which is itself the tell: the code that
was written while reading the clause got it right, and everything downstream of a summary got it wrong.

**A tooling bug inside the sweep, which is the part worth remembering.** The first pass reported clean and was
not. Its file-discovery pattern was `§?(15\.14\.4|…)`, and `§` is TWO UTF-8 bytes (C2 A7). In git grep's
byte-oriented ERE the `?` binds to the trailing byte only, so the pattern effectively REQUIRES C2 and can never
match a citation written bare as `15.14.4`. Two sites in `v59_byte_image.cob` — a golden, where a wrong citation
is most likely to be copied onward — survived because of it, and were caught only by re-checking with a pattern
that had no multi-byte optional in it. A sweep that reports zero is a claim, and the claim needs its own check;
"the grep found nothing" and "there is nothing" are different statements.

Behaviour is untouched — every edit is a comment or prose. Rebuilt and re-ran the affected legs to prove it:
V59 goldens 3/3, the image/codec/byte-form/notice unit filter 333/333.


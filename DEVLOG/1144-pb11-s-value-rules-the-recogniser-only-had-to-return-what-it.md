## Entry 1144 — 2026-08-02 14:48 PDT — PB11's value rules: the recogniser only had to return what it already knew

PB11's first half (DEVLOG 1139) built `DateTimeFormatGrammar` and closed rule 2 of all seven format-taking
functions — "is argument-1 one of the §15.3 formats, and a kind this function admits". The queue recorded the
second half as blocked on the recogniser's "SUBFIELD breakdown, which it does not yet return". That turned out to
be almost free, and for a reason worth writing down.

### The fact was computed and then thrown away

`IsTimeOfWidth` had always decided local-vs-UTC-vs-offset — it *had* to, because "is this a time format" is
answered by trying all three shapes (§15.3.3.4–§15.3.3.6). It then collapsed the three into a `bool`. The whole
structural change is returning `DateTimeFormatInfo(Kind, Zone)` instead. The alternative — re-inspecting the
format string at the binder call site to ask "does it end in Z or carry +hhmm" — would have been the same rule
written down in two places, which is the failure mode that produced PB4 and PB3.

`DateTimeZone.None` is deliberately distinct from `Local`: a DATE format has no time portion at all, and §15.40.3
r6 / §15.41.3 r5 are predicates on the *time portion*. Collapsing them would let a date format silently answer a
question about a time.

### Reading the rules beat reading the summary — twice

The queue's own summary said "§15.40.3 r4/r5 (argument-3 in [0,86400), |argument-4| ≤ 1439)". Both halves of that
parenthesis are wrong in ways that matter:

- **r4 does not state a range.** It says "Argument-3 shall be a value in **standard numeric time form**" — a
  defined term.
- **That term's range is fixed by the §7.3.17 LEAP-SECOND directive, not by the length of a day.** OFF (the
  default, and the only mode we support) gives `0 ≤ v < 86,400`; **ON gives `< 86,401`**. So the constant is
  cited to the directive rather than written as a bare 86400 — which turns "we don't support LEAP-SECOND ON"
  into a documented gap at the exact line that would have to change, instead of a magic number that happens to
  be right today.

### What landed, and where each rule belongs

- **§15.40.3 r6 / §15.41.3 r5 — BIND time** (`COBOLNET1633`). The offset argument is barred when the format's
  time portion is local. Decidable at compile time because rule 1 makes argument-1 a LITERAL and the argument's
  presence is syntactic. Before this it bound cleanly and was **silently discarded**.
  ⚠ The screen is **one-sided on purpose**. The converse is explicitly legal: omitting the argument for a
  UTC/offset format "shall be evaluated as though 0 were specified" (§15.40.3 r7 / §15.41.3 r6). A symmetric
  screen would reject conforming source, so the positive golden pins the omitted form in both functions.
- **§15.40.3 r5 / §15.41.3 r4 — RUN time.** `|offset| ≤ 1439`, enforced nowhere before. The boundary renders as
  `+2359`, which is precisely what the spec's own NOTE says the number means: 23 hours 59 minutes, one minute
  less than a day. Pinning the boundary rather than only the violation is what proves the check is `<=` and not
  `<`.
- **§15.41.3 r3 / §15.40.3 r4 — RUN time.** The seconds argument in standard numeric time form. `100000` seconds
  (≈27.7 hours) used to produce `hh=27` — a fabricated time with no exception condition, which is the failure
  mode PB11 exists to close rather than mere over-acceptance. The bound is scaled UP into `Int128` rather than
  scaling the argument down, because the argument arrives as (unscaled, scale) and dividing it down would
  truncate a fractional overshoot straight into range.

### Residue, named rather than silently dropped

§15.3.1.3/.5/.7's permitted-value ranges are about the DATA inside a formatted string on the PARSING side
(INTEGER-OF-FORMATTED-DATE, TEST-FORMATTED-DATETIME, SECONDS-FROM-FORMATTED-TIME), which is a different seam from
the FORMATTING side closed here; `CobolDate.Analyze` already carries per-field bounds that cover part of it. I did
not fold it in, because doing so would have been a guess about how much is already covered — it needs its own
reading of those three clauses against `Analyze`, and it is recorded on the queue entry as such.


## Entry 1276 — 2026-08-09 16:50 PDT — PB59 family 5a: CONVERT reads its format by position, and the screens land on the usage axis the rules actually name

(The wall clock reads 16:50 while entries 1274/1275 carry same-day 19:xx stamps — the skew is theirs or
the machine's; this entry is stamped from `date` per the rule and is NEWER despite the smaller number.)

The session opened on the frozen 5a apply plan and closed its landing in one pass: `BindConvert` now
walks §15.19.2 BY POSITION — slot 0 is ALWAYS argument-1 and binds as an operand, so `01 NAT PIC XX`
+ `CONVERT(NAT HEX ANUM)` compiles and prints A (the §8.10 context-sensitive words are user-defined
words where the format does not permit them; the old order-free harvest reported "0 operand + 3 format
keyword(s)"), while the two measured accept-bugs — `CONVERT(ANUM WS-A ANUM HEX)` and
`CONVERT(ANUM ANUM HEX WS-A)`, both of which compiled and printed 41 — now reject with 1514. MinArgs
2→3 on the catalog row, the DeliberatelyUnscreened["CONVERT"] entry rewritten to the enforced-in-full
disposition.

The screens did NOT land as frozen, and that is the entry's real lesson. The plan's r5/r6 screens were
CLASS-keyed ("class alphabetic or alphanumeric"); re-deriving at apply (rule 1) showed the rules key on
REPRESENTATION — r4 says "of display or national usage" in its own words, and r5's NOTE ("distinct from
simply requiring the string to be of class alphanumeric") cuts by what the storage HOLDS. A class
screen mis-answers in both directions: it rejects a numeric DISPLAY item that IS a valid string of
alphanumeric characters (the new D9 corpus row pins `CONVERT(WS-D ANUM ANUM HEX)` of `PIC 9(3)` →
303035), and the corpus itself held the counterexample — `argument_rule_equivalences_batch1`'s
NATIONAL/NAT legs rode a PIC X item, non-conforming under r6 either way; the golden's declaration was
corrected to PIC NN with the printed value byte-identical (UTF-16BE "AB" → 00410042 from either).
The landed shape is the SHARED `IntrinsicArgumentRules.StaticUsageOf` — the usage axis the mechanism
map already demanded for §15.12.3 r1, so BASECONVERT's screen set reuses it — read inline by
BindConvert for r4 (display|national), r5 (display), r6 (national) and r7 (the exclusion list; the
pre-fix probe of `CONVERT(WS-P ANY ANUM HEX)` hex-dumped the ManagedPointer's .NET TYPE NAME, which is
as wrong as a wrong answer gets). The apply-plan doc carries the dated correction block.

Evidence: nine CLI probes before, nine after; wave-local Conformance `~Intrinsic|~Corpus` 711/711 and
Unit `~Intrinsic` 49/49 with the fifteen new/extended theory rows confirmed EXECUTED by name-filter
(15/15); SpecTraceability 10/10 after `record_verdicts.py` flipped FMT-15.19.2 / AR-15.19.3-5 /
AR-15.19.3-6 → CONFORMS (GAP 4166 → 4163; AR-15.19.3-7 leg (a) enforced, row open on (b)). Measured
residue routed to 5b, not fixed casually: `CONVERT(WS-A ANUM NAT HEX)` returns 41 where §15.19.4 r4
derives 0041 — and the mechanism map's own predicted golden "4100" was wrong twice over (byte order AND
channel), one more instance of derive-from-the-rule-not-the-note. Next: 5b (the ANY raw-storage
channel, r2/r4 zero-bit padding, the runtime signature), then family 7.


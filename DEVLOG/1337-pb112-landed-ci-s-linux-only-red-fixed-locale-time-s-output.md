## Entry 1337 — 2026-08-19 18:33 PDT — PB112 LANDED: CI's Linux-only red fixed — LOCALE-TIME's output depended on the HOST's ICU version (U+202F vs U+0020 before the day period); DETERMINATION L10 addendum: pattern spacing normalizes to the plain space, so the same program prints the same bytes on every host

**The break.** CI had been red for four consecutive runs (since T4, `b13d4383`) on exactly ONE job — the LINUX
corpus-nist conformance shard — on exactly ONE test: `2014/pb64t4_locale_functions`. The Windows corpus-nist shard
stayed green throughout. The xUnit assert message showed two strings no eye could tell apart (`…09 PM]…` vs
`…09 PM]…`, caret at position 175); the TRX artifact the PB108 CI work uploads carried the bytes: expected U+0020,
actual **U+202F NARROW NO-BREAK SPACE** — ICU 72 / CLDR 42 moved en-US's standard time formats to the narrow
no-break space, the GitHub Ubuntu runners' ICU has it, and Windows' bundled ICU does not. The golden was recorded
on Windows; measured, not reasoned (WSL could NOT reproduce — its distro ICU is older, so the failing-direction
proof is CI itself plus a synthetic-pattern unit pin).

**The root cause, not the symptom.** DETERMINATION L10 read `d_fmt`/`t_fmt` from the host culture's
`ShortDatePattern`/`LongTimePattern` VERBATIM — including whatever spacing that host's ICU vintage writes into
them, so the SAME program printed different bytes on different machines. That is the exact disease owner decision
Q4 removed from collation ("our own derived tables … so the order is identical on every host"), back in LC_TIME.
Neither re-recording the golden on Linux (Windows then fails), normalizing in the test harness (masks real output
divergence), nor accepting either byte (weakens the pin) touches the cause.

**The fix — an L10 addendum, one method at the one seam.** `LocaleFacts.NormalizeSpacing` (U+202F, U+2009 → the
plain space) applied in the `DateFormat` / `TimeFormat` getters — the ONE place a `CultureInfo` is read; every
consumer (LOCALE-DATE, LOCALE-TIME, LOCALE-TIME-FROM-SECONDS) formats through them. Grounds, documented in
CONFORMANCE.md §4 item 5's T4 block and the design's §8 LC_TIME row: (1) HOST-STABILITY — a separator character
inside a fixed COBOL text field must not depend on where the program runs; (2) the survey-on-latitude rule — ISO
9945's own locale data (`t_fmt` `%r`) and GnuCOBOL's strftime rendition both use the plain space. The AM/PM WORDS,
month names and digits still come from the culture (L10 unchanged); only the volatile spacing characters normalize.

**Verification.** `CobolLocaleTests.TimePatternSpacing_IsHostStable`: the normalizer on a synthetic U+202F pattern
(host-independent — this is the arm that fails on an unfixed tree everywhere), `For("en-US").TimeFormat` free of
U+202F/U+2009 on every host, and the end-to-end byte pin `Time("130509","en-US") == "1:05:09 PM"`. The T4 golden
re-run under WSL (Linux .NET) matches `pb64t4_locale_functions.out` byte-for-byte; on Windows the full
CobolLocaleTests and the wave-local gate are green. CI's own corpus-nist Linux shard is the real proof — watched
green on this commit. kb/Work PB112 (landed, this commit); battery #26 still owed for the accumulated batch
(now PB111 + PB110/PB109 + PB112 — plan §0 ⑤ unchanged).


## Entry 1139 - 2026-08-02 02:45 PDT - PB11: the date/time format grammar becomes a recogniser, and the corpus catches me inferring by analogy

Nineteen of the batch's eighty-two findings were one missing thing. `CobolDate.Tokenize` validated a format
CHARACTER-WISE — each character in a known class, each run of a permitted width — so any string assembled from
legal pieces was accepted, and §15.39.3 r2 / §15.40.3 r2 / §15.41.3 r2 ("the content of argument-1 shall be a
date / combined / time format") were enforced nowhere. The failure mode was not over-acceptance; it was a
FABRICATED value:

```
FUNCTION FORMATTED-DATE("hhmmss" 100000)            -> "000000"     a TIME format in a DATE function
FUNCTION FORMATTED-DATE("YYYY-MMDD" 100000)         -> "1874-1016"  an extended/basic CHIMERA
FUNCTION FORMATTED-DATE("NONSENSE" 100000)          -> "      "     garbage in, spaces out
FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThhmmss" …)  -> extended date joined to a basic time
```

**The set of legal formats is CLOSED, so membership is the test.** §15.3.1.1 enumerates six date formats;
§15.3.2 gives twelve time formats (four common-time shapes × local / UTC / offset); §15.3.4 makes a combined
format a date format, an uppercase `T`, and a time format — **basic with basic, extended with extended**. Every
counter-example above is built from individually legal subfields, which is exactly why no amount of per-field
checking substitutes for asking whether the whole string is one of the formats. And §15.39.3 r1 makes argument-1
a LITERAL, so the whole question is decidable at bind time and the diagnostic is a compile error.

**The corpus caught me inferring three rules by name analogy.** I wrote the screen as "the kind this function
requires" — one value — and filled the three sibling functions in from their names. `TEST-FORMATTED-DATETIME`
became "combined". §15.92.3 r2 actually says "either a date format, a time format, or a combined date and time
format", and the existing legal corpus program `2014/formatted_datetime` uses `TEST-FORMATTED-DATETIME("YYYYMMDD")`
— so my screen rejected working, conforming source. Two more were wrong the same way: §15.48.3 r2 admits
date-or-combined, §15.79.3 r2 time-or-combined. The required kind is a SET.

That is the same shape-error I had flagged in `IntrinsicArgumentRules.Verified` earlier the same day — one value
where the rule names a set — committed by me within hours of writing the criticism. The difference is only that
the corpus had a program exercising the legal form. Where it does not, this class of error lands silently, which
is an argument for reading all seven rules before writing the table rather than four of them and a pattern.

**PB16 is retired by construction rather than separately fixed.** §15.3.3.2 makes the seconds-fraction maximum
implementor-defined and requires it to be at least nine. It is now documented at eighteen (`CONFORMANCE.md`
item 87) — chosen because that is exactly where the value stops being exactly representable, so the documented
bound and the representable range are the SAME number instead of two constants free to drift. A 19-digit
fraction is now a compile error instead of an emitter overflow printing garbage.

**What PB11 still does not do**, stated so the half-fix is not mistaken for the whole: the recogniser answers
"is this a format", and says nothing about the DATA. §15.40.3 r4/r5/r6 and §15.41.3 r3–r5 (argument ranges, and
an offset argument supplied where the format has no offset portion) and the §15.3.1.3/.5/.7 permitted-value
ranges are still unenforced. They need the recogniser to return its SUBFIELD breakdown, not just a verdict —
which is the natural next increment now that the classification exists.


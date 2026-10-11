# CobolSharp Developer Log

A chronological narrative of building a production COBOL compiler from the ISO/IEC 1989:2023
specification, targeting .NET. This log captures the thinking, decisions, failures, breakthroughs,
and lessons learned — intended as source material for a series of articles.

---

> **Ordering: DESCENDING — newest entry FIRST.** The latest entry is the `## Entry` block immediately below this line; the oldest is at the end. Add a new entry by inserting it directly under this note.
>
> **Timestamps (required, going forward):** every NEW entry's header carries a real date+time stamp —
> `## Entry NNN — YYYY-MM-DD HH:MM TZ — Title` (24-hour local time; get it from `date "+%Y-%m-%d %H:%M %Z"`, e.g.
> `2026-06-09 13:01 PDT`). The time gives the per-day granularity older entries lack, so same-day entries are always
> ordered/renumber-able. (Entries 001–511 predate this rule — many are undated and none have a time; left as-is.)


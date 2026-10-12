# CobolSharp Developer Log

A chronological narrative of building a production COBOL compiler from the ISO/IEC 1989:2023
specification, targeting .NET. This log captures the thinking, decisions, failures, breakthroughs,
and lessons learned — intended as source material for a series of articles.

---

> **One file per entry; the narrative is rendered.** Each entry is its own file in this directory,
> `NNNN-<slug>.md`, whose first line is its header. `python scripts/devlog.py render --out DEVLOG.rendered.md`
> prints this page and then every entry, **newest (highest number) first** — the descending log — and that file is
> gitignored. Two landings never edit the same file (kb/Work PB3025).
>
> **Adding an entry:** write `pending-<slug>.md` here with the first line `## <Title>` (no number, no stamp) and the
> narrative below it, and commit it with the work. The landing numbers and stamps it with
> `python scripts/devlog.py stamp <branch>`, which takes the number from `scripts/orchestrator/alloc.py devlog` and
> completes the header as `## Entry NNNN — YYYY-MM-DD HH:MM TZ — Title` (24-hour local time). `devlog.py check`
> refuses a landing without exactly one new stamped entry numbered above main's.
>
> **The history** (entries 001–1962, split from the single `DEVLOG.md` on 2026-10-11, byte for byte) keeps its
> headers as written: entries 001–511 are undated or carry no time, 1191a–1191d carry a letter, and twenty numbers
> were used twice (008–014, 171–185); a letter in the FILE name keeps each pair in its original order.


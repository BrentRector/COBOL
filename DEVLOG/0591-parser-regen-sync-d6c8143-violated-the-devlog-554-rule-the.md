## Entry 591 — 2026-07-03 18:59 PDT — Parser-regen sync: d6c8143 violated the DEVLOG-554 rule (the TYPE-gate .g4 fix landed WITHOUT its regenerated parser)

**Session-open finding:** the working tree carried a 2-line diff in `src/Cobol.Net.Frontend/Generated/CobolParserCore.cs`
— the fresh Windows-build regen of the `typeClause` predicate (`is2023()` → `is2002()`, the DEVLOG-586 TYPE-gate fix
at `CobolData.g4:249`). `git log` proves the violation: the `.g4` change landed in d6c8143 but the Generated parser
was last committed at 8c1eac1 (DEVLOG 577) — **the DEVLOG-554 CI rule was broken** (Linux CI's ANTLR regen fails and
silently falls back to the checked-in parser, so CI has been running the STALE `is2023()` TYPE gate since d6c8143).
Windows builds regenerate locally, which is why every suite stayed green here and the miss was invisible.

**Fix:** commit the regen (verified byte-identical to what the committed `.g4` produces — the build regenerated it,
and rebuilding leaves it stable). Gate: full solution build 0W/0E; conformance **1195/1195** + unit **38/38** green.

**Lesson (process):** the DEVLOG-554 rule needs a mechanical guard, not memory — the queued W3 item "path-portable
regen fix" removes the Linux fallback entirely; until it lands, any `.g4`-touching commit must `git status` the
Generated tree afterward. (Transparency: the miss was the prior session's — the P2.5/P2.6/P2.7 commits all built on
Windows and none noticed the untracked regen diff.)


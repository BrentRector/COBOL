## Entry 1140 - 2026-08-02 03:19 PDT - Closing the session: what §0 had to be rewritten to say, and two process failures worth more than the fixes

Three commits landed after entry 1139 with no DEVLOG entry of their own — the inventory merge for PB11's seven
rules, the §0 rewrite, and a doc regeneration. This entry covers them, because the DEVLOG is the only history
and two of the three are the kind of thing that must not live solely in a commit message.

**§0 had accreted back into a chronology, four days after being fixed for exactly that.** It still announced
"PB8 OPEN" several items after PB8 landed, and carried "today" / "the same day" markers that mean nothing to a
session which did not live through the day. NEXT item 1 is now a TABLE of the open queue in the order to work
it, each row stating the NEXT ACTION rather than the history, plus four STANDING CAUTIONS separated out because
they outlive the items that produced them. The failure mode is worth naming: §0 decays not by going stale but by
accumulating true statements. Every line I added was accurate when written; the document still became unusable,
because a resuming session needs "what do I do" and gets "what happened". Appending to §0 is almost always
wrong — the edit is nearly always a REPLACEMENT.

**PB11 shipped with the doc-sync gate RED, and I did not notice for two commits.** It added the COBOLNET1631
descriptor and never regenerated `docs/DIAGNOSTICS.md`; `DiagnosticRegistryDriftTests` exists precisely to catch
that. I missed it because I ran the full CONFORMANCE leg after PB11 — 4164/4164, genuinely green — and the guard
is a UNIT test. **The wave-local gate has to cover the legs the change can BREAK, not the biggest leg
available**, and "I ran the big one" is not the same as "I ran the right one".

**Then I gated on the wrong thing, in the commit that was about making §0 resumable.** I ran
`dotnet test … | tail -2 && git commit`, so the `&&` tested TAIL's exit status. The verdict line read
`Failed: 1` and the commit went through anyway. §0 documents this exact trap in two places — "Never `| tail -N`
a guard verdict: redirect the FULL output to a file, then grep the verdict line", and "gate on the VERDICT LINE,
never the exit code". Knowing a rule, having just re-read the document containing it, and writing a commit about
that document, were all insufficient; only the FORM prevents it. The re-run was done the prescribed way —
redirect to a file, read the verdict, scan for reds — and that is the form to use every time, not the shortcut
that happens to be one line shorter.

**Also folded in:** the queue's LANDED header, which §0 designates as the tally, was itself stale (it announced
PB10–PB16 as newly opened and did not know PB16 was retired or that three items are half landed). A register
that another document points at as authoritative is exactly the one that must not drift.


## Entry 030 — 2026-03-14: Debugging — PERFORM Missing DOT

**Failure:** PERFORM inside sections fails while MOVE/DISPLAY/OPEN work fine.

**Diagnosis by progressive isolation:** Wrote test programs adding one statement at a time.
Found that PERFORM was the ONLY statement missing `DOT?`. All other statements consumed the
sentence-ending period; PERFORM didn't, so the next statement saw a period where it expected
a keyword.

**User's analysis was precise:** Same root cause pattern, immediately identified.
Also fixed performTarget ambiguity (factored common prefix) and added inline PERFORM form.

---


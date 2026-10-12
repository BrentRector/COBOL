## Entry 008 — 2026-03-13: AI Misstep — Changing Source Instead of Fixing the Compiler

### What Happened

While creating the Phase 3 demo program (DEMO3.cob), the COBOL source failed to compile. The program contained `DISPLAY "5. COPY preprocessor enabled"` — the word "COPY" inside a string literal triggered the COPY preprocessor, which tried to expand it as a copybook reference, corrupting the source.

**The correct response**: Recognize that the COBOL source is valid, diagnose the preprocessor bug (naive text scanning doesn't respect string literal boundaries), and fix the preprocessor.

**What Claude actually did**: Spent multiple iterations modifying the demo source — removing the comment line, changing string content, simplifying DISPLAY text, removing features from the demo — trying to find a version that compiled. This is exactly backwards. The user had to intervene and redirect: *"This sounds like a compiler bug that we should fix instead of reworking the demo."*

### Root Cause of the Misstep

The AI defaulted to the path of least resistance: change the input to match the tool's behavior, rather than fixing the tool. This is a natural instinct when *using* software — you work around bugs. But we are *building* the software. Every compilation failure of valid source code is a bug report, not a user error. The failure IS the diagnostic.

### The Actual Bug

`FindCopyStatement()` and `FindReplaceStatement()` in the COPY preprocessor scanned raw text for keywords without tracking whether the current position was inside a string literal. Any occurrence of "COPY" or "REPLACE" — even inside `"..."` — would trigger preprocessing.

Fix: Added string literal boundary tracking (single/double quotes with escaped quote handling) to both scanner methods.

### Lesson

When building a compiler and the source fails to compile:
1. Is the source valid COBOL? If yes → it's a compiler bug
2. Fix the compiler, not the source
3. The error message tells you where in the compiler pipeline the bug lives

This is now recorded as a hard process rule for future sessions. It's also a useful data point for the article series on human-AI collaboration: the AI's instinct to modify inputs rather than fix tools is a pattern worth documenting. It required explicit human intervention to correct the approach.

---


## Entry 002 — 2026-03-13: Expanding the Key Technical Decisions

### Why This Matters
The initial plan had a one-line-per-decision summary table for technical choices. That's fine for
quick reference but terrible for understanding *why* we made each choice. Since this project will
become an article series, and since future sessions need to understand the reasoning (not just the
conclusion), we expanded every decision into a full analysis.

### The Interesting Tensions

**The "transpile to C#" temptation (KTD-4):** It's genuinely appealing — let Roslyn handle all
the hard CIL work, and we just emit C# source. But COBOL's control flow kills this idea. How do
you express `PERFORM paragraph-a THRU paragraph-d` in C#? It means "execute all paragraphs from
a through d in source order, then return." There's no C# construct for that. You'd need labels
and gotos, computed dispatch, or some state-machine transformation — all of which produce
unreadable C# that's impossible to debug. Going straight to CIL means we can emit exactly the
branch instructions we need.

**The decimal problem (KTD-5):** This one has layers. The naive approach is "just use .NET
`decimal` for all numeric data." But then you hit REDEFINES — where a numeric field and an
alphanumeric field share the same memory location. Or group MOVE — where a group item containing
numeric and string subfields is bulk-copied as raw bytes. COBOL programs *routinely* inspect and
manipulate the byte-level representation of numeric data. You can't do that with a `decimal`.

So we went dual-layer: `byte[]` for storage (preserving the byte-level semantics COBOL depends
on), `decimal` for computation (leveraging .NET's built-in base-10 arithmetic). The
marshal/unmarshal cost is the price we pay — but it only occurs on arithmetic operations, which
are a small fraction of most COBOL programs' execution time (MOVEs dominate).

**Parser generators vs. hand-written (KTD-3):** This is the decision most likely to be
second-guessed. ANTLR is powerful and would save us thousands of lines of parser code. But we
identified five specific ways COBOL breaks parser generators — PICTURE clauses, Area A/B
rules, COPY/REPLACE preprocessing, PERFORM THRU scoping, and implicit scope terminators. Each
one requires context that grammar-driven parsers don't naturally provide. Roslyn's team made the
same choice for C# (which is far less context-sensitive than COBOL), and their reasoning
convinced us.

### What's Next
Same as before — Phase 1 implementation. But now the plan document is robust enough that someone
reading it cold understands not just *what* we're building, but *why* every major choice was made.

---


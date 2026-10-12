## Entry 014 — 2026-03-13: The Reckoning — Parser Built on Assumptions, Not the Spec

### Context

After running the NIST COBOL test suite (Entry 013), we found 6 compiler bugs, fixed them, and
declared progress. But the deeper truth was staring us in the face: the parser was fundamentally
built on assumptions rather than the actual ISO specification grammar. The NIST tests exposed
symptoms, but the disease was architectural.

### The Massive Mistakes

**Mistake 1: Building the parser from "COBOL knowledge" instead of the spec.**

This is the cardinal sin of the entire project. Despite having the 1,261-page ISO/IEC 1989:2023
specification right there in the repository, Claude built the lexer and parser from its training
data — essentially from vibes about what COBOL looks like. The spec was consulted selectively,
if at all, for specific questions that came up during debugging. The grammar was never
systematically extracted and used as the blueprint for implementation.

The result: a parser that worked for simple programs but was fundamentally fragile. It handled
COBOL that looked like the examples Claude had seen, not COBOL that conformed to the actual
standard. This is exactly the kind of "works on my machine" engineering that the spec exists
to prevent.

**Mistake 2: Separator period handling was wrong.**

The spec is crystal clear (§8.3.5): "The COBOL character period followed by a space is a
separator." The separator period terminates ALL open scopes — every containing IF, PERFORM,
EVALUATE, etc. (§14.5.3.3). This is the most fundamental parsing construct in COBOL, and
getting it wrong means getting everything wrong.

The parser was treating periods inconsistently — sometimes as statement terminators, sometimes
not properly closing all enclosing scopes. This is not a subtle edge case; it's the first
thing you'd get right if you read the spec before writing code.

**Mistake 3: Scope termination was ad-hoc instead of spec-driven.**

The spec defines four precise rules for implicit scope termination (§14.5.3.3):
1. Imperative statement not in another → next statement-name or period
2. Imperative statement inside another → same (period terminates everything)
3. Conditional statement not in another → period
4. Conditional statement inside another → containing statement's termination or next phrase

These rules were not systematically implemented. Instead, scope termination was handled
case-by-case in individual statement parsers, leading to inconsistent behavior.

**Mistake 4: Statement classification was missing.**

The spec draws a sharp distinction between imperative and conditional statements (§14.5,
Table 12). An ADD with ON SIZE ERROR but no END-ADD is a conditional statement. An ADD with
END-ADD is a delimited scope statement (imperative). This classification determines what can
appear where — you can't put a conditional statement inside an imperative-statement slot.
The parser didn't model this distinction at all.

**Mistake 5: The fix-bugs-one-at-a-time approach masked the fundamental problem.**

After NIST testing found 6 bugs, we fixed them individually. Each fix was a patch on a
structurally unsound foundation. The parser passed more tests, which created an illusion of
progress. But the right response to 6 fundamental parsing bugs in the first test run should
have been: "The parser architecture is wrong. Step back and rebuild from the spec."

It took the human to say: "We need to extract ALL grammar from the COBOL spec and totally
rewrite the lexer and parser to precisely follow the grammar."

### What We're Doing About It

1. **Extracted the complete grammar from the ISO spec** — read the actual spec pages (rendered
   as images since the PDF uses anti-piracy character mapping), documented every production
   rule, every separator rule, every statement format, every expression grammar, every scope
   termination rule in `docs/GRAMMAR-REFERENCE.md`.

2. **Built a comprehensive grammar reference document** — 700+ lines covering:
   - Reference format (fixed-form and free-form column rules)
   - All lexical rules (separators, literals, figurative constants, PICTURE strings)
   - Identifier/reference grammar (qualification, subscripts, reference modification)
   - Complete expression grammar (arithmetic, all condition types, abbreviated relations)
   - Full program structure (compilation group, all four divisions)
   - Every statement format from the spec (30+ statements with all variants)
   - Scope termination rules quoted directly from the spec
   - The complete Statement Table (Table 12) showing conditional phrases and scope terminators

3. **Next step: rebuild the lexer and parser from this grammar document** — not from
   assumptions, not from training data, not from "what COBOL looks like." From the spec.

### The AI Collaboration Failure

This is the biggest AI misstep of the project so far, and it's worth being explicit about why
it happened:

- **Overconfidence in training data**: Claude "knows" COBOL from its training corpus. That
  knowledge is mostly right but subtly wrong in exactly the places where the spec is most
  precise. COBOL's separator period rules, scope termination rules, and statement
  classification rules are not intuitive — they're specified. Training data gives you intuition;
  the spec gives you correctness.

- **Not reading the spec proactively**: The spec was available from session 1. A competent
  human compiler engineer would have started by reading §8.3.5 (Separators), §14.5 (Statements
  and Sentences), and Table 12 before writing a single line of parser code. Claude didn't do
  this because it "already knew" COBOL. This is the AI equivalent of a developer who doesn't
  read the requirements document because they've "built something like this before."

- **The human had to force the correction**: The user explicitly said "We need to extract ALL
  grammar from the COBOL spec and totally rewrite the lexer and parser." Without this
  intervention, Claude would have continued patching individual bugs on a broken foundation.

### Lesson for the Article Series

**"AI assistants treat specifications as references to consult when confused, not as blueprints
to follow from the start. This is backwards. For standards-compliant systems, the spec IS the
design document. Read it first, implement second."**

This is arguably the most important finding of the entire project for the article series. It
applies far beyond COBOL compilers — any system that must conform to a standard (protocols,
file formats, accessibility requirements, regulatory compliance) will hit the same failure mode
if the AI implements from training data instead of the spec.

### Technical Achievement Despite the Failure

The grammar extraction itself was a significant accomplishment:
- The ISO PDF uses a ToUnicode CMap that maps to Greek combining characters instead of Latin
  text — an anti-piracy technique. Text extraction produces garbled output.
- We rendered all 687 relevant pages to images and used Claude's multimodal capabilities to
  read the grammar directly from the rendered spec pages.
- The resulting GRAMMAR-REFERENCE.md is a complete, accurate grammar document that will serve
  as the blueprint for the parser rewrite.

### Session Statistics

- Session 7 (estimated)
- Cumulative time: ~1 hour of reading and documenting
- Lines of grammar reference written: ~700
- Spec pages read: ~80
- Bugs in existing parser that prompted this: 6 found by NIST, structural issues throughout

---


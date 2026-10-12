## Entry 941 — 2026-07-20 23:25 PDT — Wave D (start): >>DEFINE §7.3.11 SR2 + AS PARAMETER + OVERRIDE + >>PUSH/>>POP recognition; the F3-runtime architecture recorded

Two threads, both under the owner's firm requirement (restated mid-session): **commercial-quality, decade-supportable
compiler** — which reshaped the plan for the remaining P13 work.

**(1) The PERFORM Format-3 RUNTIME interceptor architecture — recorded, deliberately NOT rushed.** After landing the
front half (DEVLOG 940) I started the runtime as the design's §5 lambda/frame approach, then STOPPED and reverted it:
for a decade-supportable compiler it is the wrong architecture. `ResumeSignal.cs` records that the as-built handler-
in-response-to-an-EC mechanism is **pc-RANGES run by the bounded dispatcher** (`__RunUse` wraps `__Dispatch(start,end)`
in `try/catch(ResumeSignal)` and returns the resume action). A WHEN handler IS an inline declarative (GR17 — a WHEN
match REPLACES a matching USE declarative), so the lambda approach — which needs an "am-I-in-a-lambda" statement-
emission mode (RESUME → return-action; `EXIT PERFORM` → a thrown signal because C# cannot `goto` out of a lambda) —
would be a SECOND mechanism for the same job (`feedback_singular_pattern` violation, a long-term maintenance hazard).
Recorded in the design SSOT's IMPLEMENTED note: the wave must choose a SIGNAL-BASED, singular-pattern option — **(A)**
synthetic pc-ranges dispatched exactly like declaratives, or **(B)** handler-methods + a frame matcher with RESUME →
`ResumeSignal` and `EXIT PERFORM` → a new `ExitPerformSignal` — and gate `__EcPerform` on "unit HAS an F3 PERFORM" so
every non-F3 program stays BYTE-IDENTICAL (the 33 characterization tests + the battery). This is a control-flow-core
change deserving a careful, reviewed wave — staging it (0899, honest, documented) is the correct production posture.

**(2) Wave D — >>DEFINE (ISO §7.3.11) production-quality fixes.** In `ConditionalCompilationProcessor`: the
**§7.3.11.3 SR2 no-OVERRIDE redefinition check** (COBOLNET1618 — a redefinition to a DIFFERENT value without OVERRIDE
is rejected; same-value and post-OFF redefinitions are allowed, per the three SR2 bullets), **AS PARAMETER** (GR4 —
the compilation variable's value is sourced from the operating environment via `Environment.GetEnvironmentVariable`;
undefined when unavailable — it previously mis-bound the literal word "PARAMETER"), and the **OVERRIDE** phrase (GR3 —
unconditional set, bypassing SR2). Threaded the frontend `DiagnosticBag`/`sourcePath` into the CC processor (the
established directive-processor pattern; optional params keep legacy callers untouched). COBOLNET1618 is a FRONTEND
`ReportError` code, so it takes a `DiagnosticCatalog` descriptor (the `EveryEmittedCode_IsACatalogDescriptor` /
DIAGNOSTICS.md drift discipline) — distinct from the F3 compiler-channel raw band. Also added **>>PUSH/>>POP** to the
recognized-directive set (§7.3.20/§7.3.22 — consumed, a faithful no-op disposition since no compiler-directive state
is currently varied; previously they reached the lexer as stray tokens). 7 new tests. STILL DEFERRED (recorded): the
§7.3.6/§7.3.7 arithmetic/boolean EXPRESSION evaluator (a multi-token operand still binds only its first token) and
CC-directives-inside-COPY.


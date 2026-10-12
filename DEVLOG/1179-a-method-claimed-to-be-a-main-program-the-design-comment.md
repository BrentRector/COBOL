## Entry 1179 — 2026-08-05 09:00 PDT — a method claimed to be a main program; the design comment that allowed it cited two real rules, neither of which governs

**PB36's headline half landed.** `FUNCTION MODULE-NAME` inside a METHOD was wrong in all three keywords, and the
worst of them was disguised as a legitimate answer.

**§15.65.4 r5** names the activation mechanisms outright: *"the returned value is the name of the runtime element
that activated the currently running runtime element. This may be by a CALL statement, **an INVOKE statement**, a
function reference, or an inline invocation."* Measured inside a method before the fix:

| keyword | got | required |
|---|---|---|
| `CURRENT` | the CALLER's name | the running element's compilation unit (r7) |
| `ACTIVATING` | **a single space** | the invoking program (r5) |
| `STACK` | the caller only — no method | the method's unit, then the chain (r9) |

⛔ **r5 reserves a single space for a MAIN PROGRAM.** So a method was not merely returning nothing useful — it was
returning the specific value that means *"I am the top of the run unit"*, indistinguishable from the real case. A
program cannot tell the difference.

## ⛔ THE JUSTIFICATION CITED TWO REAL RULES AND NEITHER GOVERNS — THE THIRD TIME THIS SESSION

`ModuleStack`'s class comment: *"OO method (INVOKE) activation … **That is conformant, not residue**: §15.65.4
r3/r4 make the returned form for method-id elements implementor-defined."*

- **r3** is about a runtime element that is **not a COBOL runtime element**. A COBOL method is one.
- **r4** is about the **FORM** of the name — and it *lists* "method-id" among the forms an implementor may
  return, which **presumes the element is on the stack**. Latitude over which name string, never over whether the
  frame exists.

This is the same shape as D-B1 citing §13.18.40.4 GR14 to justify not implementing `USAGE BIT` (entry 1177) and
PB24's entry citing r9 for a rounding step §8.5.1.6.3 actually decides (entry 1177 again). **`cite.py --check`
passes on every one of them**, because the text really is in the standard under that number. The check that
catches this class is not mechanical: *does the cited rule answer THE QUESTION BEING ASKED?* Three for three this
session, all in comments written to explain why something need not be done.

## ✅ THE FIX — one push, in the method body

The frame is pushed inside the emitted method in a `try`/`finally`, **not at the INVOKE site**. A method is
reached three ways — a typed direct call, the universal `__CobolInvoke` switch, an inline invocation — so a
per-site push would be three arms of one dispatch, which is this compiler's most reproducible defect shape. The
`finally` is load-bearing: a method exits by RETURNING, by GOBACK unwinding as `MethodReturn`, or by an exception
reaching the invoker, and a leaked frame would make every later MODULE-NAME read one element too deep.

`I-CUR=[CPB36MN]` · `I-ACT=[PB36MODNAME]` · `I-STACK=[CPB36MN;PB36MODNAME; ]`, and the frame pops. CALL and
function-reference activation are byte-identical and are pinned in the same golden as regressions.

## ⛔ THE OPEN HALF, WHICH THE ENTRY'S REFUTER HAD ALREADY CALLED

The entry recorded that a refuter *"refuted the adjudicator's load-bearing claim that the STACK arm is already
right"*. It is still not right, and the fix produced a measurement that shows it contradicting itself:

```
D-ACT   = [SHOW]                     <- ACTIVATING inside a method invoked BY a method
D-STACK = [CPB36MN;PB36MODNAME; ]    <- but STACK's second entry is the PROGRAM, not SHOW
```

**r9**: *"The series of module names following the first one are the names of the runtime elements that would have
been returned if the ACTIVATING keyword were specified within the previous module in the list."* Read literally
the second entry must equal what ACTIVATING returns inside the first. It does not, because the STACK builder
**collapses consecutive frames sharing a compilation unit** — a rule written for NESTED PROGRAMS, where it is
right, that also swallows two methods of one class.

⚠ **I did not "fix" it by deleting the collapse**, because that would change nested-program output the collapse
exists to keep correct. The real question is whether a STACK entry is a COMPILATION UNIT (today) or a RUNTIME
ELEMENT (r9's wording, and what ACTIVATING already returns), and any answer has to hold for nested programs and
methods simultaneously. That needs the r7/r9 interaction read properly rather than a quick edit, so PB36 stays
`half` with the measurement recorded. The golden pins today's value on a line explicitly labelled as *recording*
it, not endorsing it — so it cannot drift silently while the question is open.

## 🔬 PROCESS — I BROKE MY OWN RULE FROM YESTERDAY, WITHIN A DAY OF WRITING IT

Entry 1175 added to §0: *do not edit compiler sources while `battery.sh` is running — phase 2 rebuilds.* I then
edited `ModuleStack.cs` with a battery in flight. The edit was comment-only, and the tempting thought was exactly
the one the rule exists to refuse — *it cannot change behaviour, so the run is still valid.* That is the same
shape as explaining away the `MSB3027` lock. Stopped the run and re-ran it whole on the final tree. The rule is
cheap to obey and the reasoning to skip it is always available.


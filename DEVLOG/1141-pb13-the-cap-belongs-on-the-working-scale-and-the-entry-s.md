## Entry 1141 — 2026-08-02 04:16 PDT — PB13: the cap belongs on the WORKING SCALE, and the entry's own recipe was wrong twice

PB13 was the last open BLOCKER: `COMPUTE R = FUNCTION EXP(70)` into a `PIC 9(31)` stored a value wrong by a
factor of about fifteen and reported NO SIZE ERROR, and `FUNCTION EXP10(30) = FUNCTION EXP10(31)` — two values a
factor of ten apart — evaluated TRUE. It is fixed, and almost nothing about the fix is what the queue entry said
it would be.

### The mechanism, which the entry described as a symptom

`CobolIntrinsics.FromDouble` lands a double at a WORKING scale and saturates at `Int128.MaxValue`. The store then
rescales working→receiver scale — and that division shrinks the saturation sentinel back INTO the receiver's
range. The digit-capacity check, the one mechanism that would have raised the size error, therefore never saw it.
**That** is why the saturation was silent rather than loud, and it is the entire defect.

The clarifying comparison was already in the tree: `CobolFloat.ToScaled` saturates too, and has always been safe,
because it lands AT the receiver's scale so its sentinel survives into the capacity check. Saturation is safe
exactly when nothing rescales it afterwards. Only the working-scale path lost that.

### The fix is entirely emitter-side, which the entry said was impossible

The entry's recipe read: "the fix is TWO-SIDED … the runtime must stop silently saturating, which no emitter-side
choice can reach." That conclusion follows only if the emitter's single lever is the working SCALE. The actual
lever is **whether to quantize at all**.

- **With a receiver** — `ReceiverContext.WorkingScale(floor)` caps the working scale at the receiver's Int128
  headroom: `ws ≤ 38 − integerDigits`. Two invariants fall out, and the second is what makes the cap sufficient
  rather than merely better: a value that FITS can no longer saturate, and a value that does NOT saturates to a
  sentinel that still exceeds the receiver after the rescale — so the store RAISES the size error (§14.7.5
  case 5). `COMPUTE R = FUNCTION EXP(700)` now reports SIZE ERROR where it used to store garbage silently.
  The runtime needed no change; the saturation became loud *by construction*.
- **With no receiver** — the render never reaches the quantizer. There is no scale to quantize TO, so the `ws = 9`
  stand-in was arbitrary. §15.4.1 leaves "the characteristics and representation of the returned value" to the
  implementor under native arithmetic, and our determination is that the float family's value IS a binary64.

The second half needed no new determination either, because **the docs were already right and the code did not
match them**. `docs/CONFORMANCE.md` says a float-valued function "renders through the same shortest-round-trip
`CobolFloat.Display` a COMP-2 item does, so the function and an item holding its value agree", and the
`da2_function_as_text` golden's own header states the form is "significant digits, no zero padding". The
working-scale path printed `2.000000000` for `FUNCTION SQRT(4)` — zero-padded, against both — and for
`FUNCTION EXP10(31)` it printed the saturation sentinel outright, `170141183460469231731687303715.884105727`.

### Two citations that were inherited rather than derived

The entry, plan §0 and a runtime doc comment all said "§14.7.4 makes an intermediate overflow a size-error
condition". §14.7.4 is the **ROUNDED phrase**. The real clause is **§14.7.5 case 5** — "if native arithmetic is in
effect and the implementor defines that the range of values allowed for the intermediate data item is to be
checked" — which is also the clause that makes our disposition a documented implementor choice under §8.8.1.3
rather than an accident. All three now say so, and every citation in this change ran `cite.py --check`.

### The sibling sweep found three more sites, and the guard found the worst of them

Rule 4 says every bug is a pattern. The entry named one site; there were four.

- **`**`** — `NumericRenderer.Power` reaches the same quantizer with the same formula, so `COMPUTE R = 10 ** 30`
  into a `PIC 9(31)` stored the identical wrong constant and `IF 10 ** 30 = 10 ** 31` was TRUE.
- **The NUMVAL family** — and this is **PB5's own defect, in the sibling PB5 never swept**. `Numval`, `NumvalC`
  and `NumvalF` still returned `long` and clamped at `long.MaxValue`: the very 9.2×10¹⁸ clamp PB5 widened to
  `Int128` in `FromDouble`, left standing three functions away. With the family's ≥6/≥9 floor it fires on
  ordinary arguments — **`FUNCTION NUMVAL-F("1E+20")` returned 9223372036**, ten orders of magnitude out, with no
  size error, where §15.69.4 r2 requires an approximation of the value argument-1 represents.
- **`NumericRenderer.Align`** — no `Real` arm, so a receiver-less binary64 reaching a subscript, a SET amount, a
  PERFORM VARYING FROM/BY, a report VARYING or a RETRY count was handed to a caller expecting a scaled integral.
  Already reachable through a COMP-2 operand, so this was a latent defect too. The arm went in at the one choke
  point rather than at forty call sites.

**The NUMVAL family was found by the drift test, not by me.** `FloatQuantizeHeadroomDriftTests` failed on its
first run against a site my own sweep had missed by eye. And its first version was itself too narrow: it matched
only `Math.Max(…Scale, 9)`, the float family's floor, and passed straight over three NUMVAL sites written at
floor 6. **The floor is part of the pattern, not part of the site** — which is why the rule is now one method,
`WorkingScale(floor)`, parameterised by the family's documented floor. A per-family copy is precisely how PB5
fixed one call site of a clamp that had four.

### What it cost, measured

The cap binds only past 29 integer digits — below that `38 − integerDigits ≥ 9` and the float floor still wins —
so the change is behaviour-neutral for every ordinary picture, and the drift test pins that so a future edit
cannot quietly re-scale every money field in the corpus. Across the whole 4164-case Conformance corpus, Fix A + B
moved exactly **one** golden: `da2_function_as_text`, from `Q1=[2.000000000]` to `Q1=[2]` — toward the
determination its own header states. The 42 NIST IF1xx intrinsic programs, which compare float intrinsics inside
IF conditions against the NIST goldens byte-for-byte, were unaffected: their tolerance bands are exactly what a
native comparison is for.

### Filed, not fixed

PB13's sweep exposed **PB18**: `10 ** 30` now returns 1000000000000000071935427891953 — the `Math.Pow`
approximation — where Int128 holds 10³⁰ exactly. It CONFORMS (§8.8.1.2 r6 imposes no exactness requirement,
§8.8.1.3 makes native implementor-defined) but it contradicts our own documented native technique, numeric design
D3, "the exact Int128 fixed-point engine". `CobolDec.Pow` already does it correctly for the standard modes, so
the shape to copy exists. It was invisible until PB13 stopped the saturation from masking it.

Design: `COBOLNET_NUMERIC_DESIGN.md` D18. Goldens: `pb13_float_quantize_headroom` (promoted from `pending`) and
`pb13_float_quantize_siblings`.


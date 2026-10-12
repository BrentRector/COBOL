## Entry 608 — 2026-07-04 20:32 PDT — The UNIVERSAL wave: __CobolInvoke switches, runtime GR7c conformance (EC-OO-UNIVERSAL), SET Format 5, object relations (D10 complete)

**What landed (the D10 brief, D-U1–D-U8; codes shifted to 0866/0867/0868 — the brief's "0836-0838
unused" predated FACTORY/OVERRIDE).** Universal object references are FULLY LIVE:

- **D-U1:** a universal reference emits as `CobolObject?` (not `object?`) — CobolObject IS the runtime
  universal type; GR2b defers non-COBOL interop; no cast at dispatch sites.
- **D-U2:** `CobolObject.__CobolInvoke(string, CobolInvokeArg[], CobolInvokeArg?)` + per-class switch
  overrides over the DECLARED-non-override roster (`default:` chains base — the chain IS §9.3.6; an
  override wins through C# virtual dispatch from the base's case, proven by oo_universal_inherit); BOTH
  type halves emit switches (a universal can hold a factory object); the root default raises EC-OO-METHOD
  (GR7b). `NormalizeMethodName` = TrimEnd + upper (GR2a/§8.3.2.2 — identifier-2 content is a user word).
- **D-U3:** `OoClassTable.ConformanceDescriptor` — ONE encoding beside DescriptionMismatch (the
  no-drift home): §14.9.23.4 GR7c makes §14.8.2/§14.8.3 conformance a RUNTIME check through a universal
  receiver (§9.3.8.2.1 NOTE); the generated switch compares descriptors for string equality and raises
  `CobolFatalException("EC-OO-UNIVERSAL", …)` on ANY mismatch — arity, per-arg, RETURNING presence both
  directions — UNCONDITIONALLY (the EC-OO-NULL/METHOD precedent; a nonconforming crossing in a
  typed-native model is never proceedable). Documented strictness deltas: no §14.8.2.2 rule-1
  group-prefix leniency through universal; JUSTIFIED encoded alpha-only; `T:!` sentinel for not-carried
  categories (bind-rejected from universal crossings before any box exists).
- **D-U6a (NEW locked decision, an as-built addition):** box forms are CANONICAL BY DESCRIPTOR — never
  by either side's storage form, because MarkStoreAsImage flips StoreAsImage PER UNIT and the two sides
  of a universal crossing are different units: `S:*` → string; `N:Display:*` → the display IMAGE string
  (the FormatDisplay/StoreDisplay overload pair bridges native↔image on each side independently);
  other `N:*` → native; `O:*` → the reference.
- **Binder (D-U5):** `BoundInvokeUniversal`/`BoundUniversalArg` (facts differ in KIND from BoundInvoke —
  no roster exists at compile time); identifier-2 routes ONLY through universal receivers (SR7), must be
  alphanumeric (SR8); BY CONTENT/BY VALUE, literal/arith arguments, OBJECT-data arguments (SR10+SR6),
  and Tier-C groups = **0866**. Both DEVLOG-601-era stages (identifier-2; universal receiver) retired.
- **SET Format 5 (D-U7) LIVE:** grammar widened to `dataReference+` ({identifier-3}…, §14.9.39 :31162) —
  and the ANTLR reality documented IN the grammar: a dataReference SENDER parses as the Format-1 shape
  (alternative order), so `BindSetTo` re-routes SEMANTICALLY when EITHER side is an object reference
  (an object SENDER into a non-object target gets the SR8 0867, not a category error). NULL/SELF senders
  + SR13 class-name senders (→ the D11 factory singleton; typed receivers 0867-named until FACTORY OF
  usage lands) come through the gated rule. SR8/SR9-SUPER/SR12 (universal-into-typed FORBIDDEN — the
  narrowing tool is an object view, the EC-OO wave; typed-into-typed ConformsTo; SELF-in-method +
  SR12c2) = **0867**. The FROZEN legacy's single-target accessor got the one-line `[0]` fix
  (oracle-only code; forensically commented).
- **Object relations (D-U8) LIVE:** Format 3 (`=`/`<>` ONLY :9591; both operands class object SR5 —
  figurative NULL rides) = **0868** violations; identity renders `object.ReferenceEquals` in the
  ConditionRenderer's object branch BEFORE the figurative branch (NULL must never width-materialize).
  **"IS class-name" STRUCK as non-ISO** (zero spec surface — the runtime-type tests are Format-3
  relations + object views §8.4.3.5/EC-OO-CONFORMANCE, deferred to EC-OO); the deep-dive mapping's
  `IF G IS GREETER` line replaced.
- **Registry/matrix/hints:** set-object-reference-2002 (registry + ACTIVE constructs.json row + the
  NULL/SELF-after-TO W1.5 hint). The `using CobolNet.Runtime.Exceptions` emission is now gated on
  `_ecActive || classes.Count > 0` — the FIRST cut emitted it unconditionally and broke the
  zero-scaffolding invariant's namespace grep (SSOT §18.16; the test caught it — the invariant held).
- **Goldens (4, greenfield-only; 23 oo_*):** oo_universal (SET U TO typed ×2 + polymorphic dispatch +
  id-2 + write-back: I-AM-A/I-AM-B/R=0012 N=0006), oo_universal_name (NEW-RETURNING-U variant),
  oo_universal_inherit (default-chain HELLO-FROM-BASE + virtual-through-base's-case DERIVED-VOICE),
  oo_universal_relation (NULL/NOT-NULL/identity-T/identity-F).
- **Tests (+11 OoSpine):** THE polymorphic hazard — two classes, same method, 9(4) vs 9(8) formals
  (both `ref long` in C#): the wrong-shaped crossing raises EC-OO-UNIVERSAL, the conforming pair runs
  (the D-U3 invariant proven BEHAVIORALLY — DataItem construction outside the binder is not a supported
  seam, so the brief's direct unit matrix was traded for the behavioral pair; noted in the AS-BUILT);
  arity; RETURNING presence; unknown → EC-OO-METHOD; null → EC-OO-NULL; 0866 ×3 theory; 0867 ×3 theory
  (incl. SET-typed-TO-universal); 0868 ×2 theory.

**AI friction (honest).** (1) A big generator patch went through a bash heredoc AGAIN and python
de-escaped every `\"` — caught by the C# compiler, repaired by direct Edit; the Write-tool patch-script
rule exists for exactly this and was violated twice this session. (2) The unconditional Exceptions using
broke the zero-scaffolding byte-invariant — the guard test caught it; gated properly. (3) The grammar
widening broke the frozen legacy's generated accessor (single→array) — one `[0]` fix. (4) The first SET
F5 re-route keyed only on the TARGET being an object reference; `SET N4 TO U` (object SENDER) fell into
Format 1 — the theory test caught it; the trigger now keys on EITHER side.

**Battery:** greenfield conformance **1580/1580** · unit **101/101** · OoSpine+Corpus+Matrix 455/455 ·
FULL legacy guard **353 MATCH — ALL GREEN** · 3-edition continuity sweep **439 OK / 20 SKIP85 /
0 BREAKS** (both baseline-exact; grammar changed ⇒ both required).



## Entry 609 — 2026-07-04 21:51 PDT — EC-OO landed: RAISE identifier, EXCEPTION-OBJECT, USE Format 4, RAISING propagation through INVOKE — ⛔🎉 the OO brief set is COMPLETE

**What landed (the LAST OO brief, D-EO1–D-EO10; codes 0848/0849/0858/0859 — the brief's 0830-0833 were
long taken).** The exception-OBJECT channel over the landed EC engine, ONE signal architecture (D-EO1 —
no parallel OO mechanism):

- **Runtime (D-EO2):** `ExceptionState.ExceptionObject` (`CobolObject?` per D-U1), the
  `"EXCEPTION-OBJECT"` LastName SENTINEL (§15.33.3 r1 — EXCEPTION-STATUS worked with ZERO function
  changes, golden-proven), `SetObject`/`SetPropagatingObject`/`TakePropagatedObject` (the object slot,
  mutually exclusive with the named slot), the `SetPropagatingLast` object leg (GR1b3a).
- **RAISE identifier-1 (D-EO3):** grammar takes `objectReference` (NULL/SUPER → targeted 0848; RAISE SELF
  parses); `BoundRaiseObject`; not TURN-gated; NEVER fatal by itself (GR2) — the first cut returned
  "terminated" from the emitter and CS0163'd on its own continue path (the backend compiler caught it).
- **USE Format 4 (D-EO7):** `USE AFTER {EXCEPTION OBJECT | EO} class-name` (EO = a context-sensitive
  word, the EC recipe); `__EcObjDispatch` — GR14a's class-or-subclass IS C#'s `is`; GR3: F4 REPLACES the
  F1/F3 tiers for object raises; unknown class = 0859.
- **RAISING identifier (D-EO4/D-EO6/D-EO8):** `BoundRaising.ObjectSource` (tri-state with EcName/IsLast);
  SR4d no-universal + SR4a declared-class-in-the-header (base-chain walk) = 0849 at COMPILE time — the
  activated-side rule-1 check is thereby STATICALLY discharged in v1 (D-EO5: no universal identifier-1
  exists and factory objects cannot enter typed references; revisit at the FACTORY-OF/interface RAISING
  legs). Headers PARTITION (level-3 EC-USER names per SR7, else 0858 / classes of the group); methods
  carry their own partition (`OoMethodSymbol.RaisingEcNames/RaisingClasses`), loaded per-method — a
  method IS a source element. `BoundMethodReturn(Raising)` stages BEFORE the throw; the entry catch
  still delivers RETURNING + copy-outs, so GR1b's result-before-exception ordering is structural
  (golden-proven: HANDLED R=0077 then AFTER-INVOKE R=0077).
- **The pickup:** `CallEmitPropagationPickup` grew the object branch FIRST — GR1b2 re-register → F4
  (rule 2) → rule-4 conversion on −3: `Set("EC-OO-EXCEPTION", true)` → the F3 tiers → unresumed ⇒ fatal
  `CobolFatalException` (Table 13). The SAME pickup runs after every CALL, every
  Instance/Self/Super/Factory INVOKE, and every UNIVERSAL dispatch (a post-brief delta — D10 landed
  in-between). Test correction en route: surviving the rule-4 conversion needs RESUME AT NEXT STATEMENT —
  EC-OO-EXCEPTION is FATAL, and the fatal protocol (declarative-completes ⇒ still terminate, #5/#7) was
  already right; the TEST was wrong, not the engine.
- **SET … TO EXCEPTION-OBJECT (D-EO9, shrunk):** the register as an F5 SENDER on the ALREADY-LANDED
  Format 5 (DEVLOG 608): universal target copies; TYPED target runtime-narrows
  (§9.3.8.2 :12291 → EC-OO-UNIVERSAL, spine-tested); the register as a TARGET = 0848 (§8.4.3.6 SR1).
  The reserved word needed a REGISTER-CONTEXT exemption from the 0901 funnel (objectReference ancestors
  + the SetToValue re-route shape — the reservation is exactly what makes the reference unambiguous).
- **Goldens (2, greenfield-only; 25 oo_*):** oo_ec_raise_object (subclass F4 match skipping a wrong
  handler + the EXCEPTION-STATUS sentinel + SET U TO EXCEPTION-OBJECT + continue-after-RAISE),
  oo_ec_goback_raising (method GOBACK RAISING through the INVOKE-site pickup, GR1b ordering).
- **Tests (+9 OoSpine):** GR2 continue; the rule-4 F3 catch (with RESUME); the wrong-class runtime narrow;
  0848 ×3 theory; 0849 ×2 theory (SR4d universal, SR4a undeclared); 0858 method-header non-EC-USER;
  0859 unknown F4 class.
- **No new registry rows:** RAISE-identifier/F4 ride the EC family's bind-side 0876 gates — the same
  precedent as the named RAISE/RESUME/F3 forms (none have rows).

**Residue (0899-named, owning waves):** PROPAGATE ON (preprocessor); interface / FACTORY OF /
ACTIVE-CLASS RAISING legs; method declaratives (RAISING LAST in a method); object VIEWS
(§8.4.3.5 / EC-OO-CONFORMANCE — the deferred narrowing surface); STOP … RAISING (no grammar surface).

**⛔🎉 With this, ALL FIVE OO slice briefs are LANDED** (FACTORY 604 · OVERRIDE/FINAL 605 ·
INTERFACE/PROPERTY 606-607 · UNIVERSAL 608 · EC-OO 609): the M2 OO port's brief set is complete. NEXT =
the Phase-4 catalog per `docs/COMPLETION_ROADMAP_COUNCIL.md` (pointers/ALLOCATE on ManagedPointer,
national/boolean, UDFs, the intrinsics gaps) toward 100%.

**Battery:** greenfield conformance **1592/1592** · unit **101/101** · OoSpine 72/72 ·
FULL legacy guard **353 MATCH — ALL GREEN** · 3-edition continuity sweep **439 OK / 20 SKIP85 /
0 BREAKS** (both baseline-exact; grammar changed ⇒ both required).



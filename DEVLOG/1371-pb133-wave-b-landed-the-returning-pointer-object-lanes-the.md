## Entry 1371 — 2026-08-29 05:01 PDT — PB133 wave B LANDED: the RETURNING pointer/object lanes (the PB111 shape on user source) and §14.9.4.4 GR3a's once-only item identification

Two mechanisms. (2) CallAbi.StoreReturn's overload set spanned the four numeric carriers and string — a
LEGAL `PROCEDURE DIVISION RETURNING` program-pointer, data-pointer, or object-reference item made the
generated callee fail Roslyn compilation (CS1503, the PB111 wrong-stage shape on conforming source). Three
lanes added: ManagedPointer (data pointer), ProgramPointer (§13.18.60 GR24's identity struct), and a
CobolObject-constrained generic for object references — the constraint keeps overload resolution away from
every numeric/string carrier, and the identically-described returning pair (§14.8.3's conforming case a
prototype-less CALL realizes) matches the typed carrier exactly; the cross-class described relationship
rides wave C's conformance campaign. Goldens: pb133_returning_pointer (PP-SET through SET … TO ENTRY of
the outermost program + DP-NULL for the never-set initial state) and pb133_returning_object (O-SET).

(3) §14.9.4.4 GR3a — "item identification is done … at the beginning of the execution of the CALL
statement", with §14.2.3 GR8 fixing each BY REFERENCE argument's storage area at the same point. The
OverField carriers re-render their subscript and ref-mod expressions on EVERY access, so a callee that
re-aimed the caller's index item through another BY REFERENCE argument moved the alias mid-call — the
RETURNING delivery and every later store landed in a DIFFERENT element than the one the statement
identified. EmitCall now hoists every non-constant table subscript and ref-mod position of the aliasing
operands into statement-locals evaluated once at the CALL's start (HoistOnceOnlyIdentification — the
value operands already read once when the args array is built). Golden pb133_gr3a_once_only pins the
mechanism: the callee sets I to 2 through the alias and stores 7777 — E1=7777, E2=0000, where the old
carriers delivered into E(2).

PB133 remains OPEN — wave C (§14.8.2/§14.8.3 conformance + EC-PROGRAM-ARG-MISMATCH, GR12's
omitted-argument gate, the image-incapable group arguments) follows.


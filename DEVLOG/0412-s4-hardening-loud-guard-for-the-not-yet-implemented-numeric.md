## Entry 412 — S4 hardening: loud guard for the not-yet-implemented numeric sender-materialize

Sibling to 411's hardening: `CilLocationEmitter.EmitLocationArgsMaterializingTyped` (used by the COMPARE +
class-condition sender paths, 407/409) also assumed the typed field is a `string` (`CobolString.ToWindow`). A
numeric typed field as a read operand — e.g. `IF WS-NUM IS NUMERIC` — would have pushed a `long` where a `string`
is expected (silent mis-emit). Flag-ON-only and unexercised (guard green), but a reachable mis-emit surface once
numeric typed fields exist (410). Now it throws a clear `NotSupportedException` for a numeric typed read operand —
the numeric sender-materialize (`EncodeNumeric` → scratch window, which will *enable* numeric COMPARE/class-cond) is
the next increment. Loud-fail over silent-corruption, per the quality bar. Guard **ALL GREEN: 1196 unit / 491
integration / 364 NIST**. NEXT: the numeric sender-materialize + numeric COMPARE, then numeric **arithmetic**.


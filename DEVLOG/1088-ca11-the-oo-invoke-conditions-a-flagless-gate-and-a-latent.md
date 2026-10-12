## Entry 1088 — 2026-07-28 16:52 PDT — CA11: the OO INVOKE conditions, a flagless gate, and a latent status bug it exposed

Step 8. Citations validated with `cite.py` before any code was read — §14.9.23.4 GR5 and GR7b both check out,
unlike CA10's.

**The rules.** GR5 — "If identifier-1 is null, the EC-OO-NULL exception condition is set to exist and execution
of the INVOKE statement is terminated." GR7b — the method could not be located → EC-OO-METHOD. Both Fatal.

**A PRECISE gate, not an ambient one:** an INVOKE is a distinguishable bound node, so `QueryFor` gets
`case BoundInvoke or BoundInvokeUniversal` and the guard binds only on an actual INVOKE under
`>>TURN EC-OO-* CHECKING ON` — unlike the CA9/CA10 families, which render inline and have no single node kind.

**The gate table now admits a FLAGLESS gate, and that is a deliberate shape rather than a shortcut.** These two
raise sites are unconditional: GR5 terminates the INVOKE, and a typed-native model has no conforming value to
return from an invocation that did not happen — the same reasoning as the null dereference in CA9. So there is
no checking flag to set, and `FatalAmbientGates` became `(string Ec, string? Flag)`. A flag nothing reads would
be state a future maintainer has to disprove; a null says outright "the guard exists only to catch and
dispatch".

**⚠ AND IT EXPOSED A LATENT BUG THAT WAS INVISIBLE UNTIL NOW.** The golden first came back with
`HANDLED=` followed by SPACES — the declarative selected and RESUMEd, but FUNCTION EXCEPTION-STATUS returned
nothing. The emitted catch only set the last-exception status on the `WITH LOCATION` arm; every fatal EC until
today set it at its RAISE SITE (the `XxxError` helpers all `Set` before throwing), so the omission never
showed. A gate whose raise is unconditional has no such site. §14.6.13.1.1 is explicit — "If checking for an
exception condition is enabled and an exception status indicator is set … the last exception status is set to
indicate that exception condition" — and the guard only exists where checking IS enabled, so the catch now
sets it unconditionally, with the location operands only when the enabling TURN carried WITH LOCATION. This
also removes a redundancy nobody would have noticed: for flagged gates the value is simply written twice with
the same content.

**Golden** `2023/ec_oo_null_invoke`, registered same commit: an object reference never given an instance,
INVOKEd. Without the fix the condition escapes uncaught and the run unit terminates with the declarative never
selected.

**Gate:** characterization 33/33 · Conformance CorpusRunner+ExceptionCondition+Oo+PerformFormat3 670/670.

**Not covered here, named:** EC-OO-METHOD has the gate and the guard but no golden of its own — reaching it
needs an INVOKE of a method the receiver's class does not define, which the binder rejects at compile time for
a typed receiver. It is reachable only through a UNIVERSAL receiver, which is V55's surface, so its golden
belongs with V55 rather than being forced here.


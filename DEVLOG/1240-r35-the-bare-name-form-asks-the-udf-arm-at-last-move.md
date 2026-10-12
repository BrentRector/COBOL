## Entry 1240 — 2026-08-08 17:29 PDT — R35: the bare-name form asks the UDF arm at last — MOVE WITHOUTPAR TO X binds

§8.4.3.2.3 SR2 permits omitting FUNCTION for a REPOSITORY-declared USER function exactly as for a
declared intrinsic, and a zero-argument function's keyword-omitted reference is a BARE NAME. PB7 built
the bare-name arm for the intrinsic catalog and never asked the UDF side — the two-arm-dispatch shape's
SIXTH sighting — so MOVE WITHOUTPAR TO X fell to the data path: compile-clean death before R30,
"undefined" after, and the differential's run_functions:4457 (GnuCOBOL's own testsuite) named it.

The fix routes a declared UDF name straight to UdfBindCall(name, []), whose prototype machinery owns
both verdicts: a zero-formal function binds (ref-mod tail included), an N-formal one draws its own
COBOLNET1506 arity error — the honest diagnosis, since the user DECLARED the name a function and there
is no coincidental-collision reading to protect. The catalogued-name arm keeps that protection: a bare
collision with SQRT stays an ordinary unresolved name.

Probes: 0042 prints; "takes 2 argument(s)" for the two-formal bare form. Goldens: udf_bare_zero_arg
(2002) + udf-bare-arity (negative, 2002/2014/2023). Gate: Udf|Corpus 555/555 · both by name ·
characterization 33/33. The R34+R27+R29+R35 batch battery launches next.


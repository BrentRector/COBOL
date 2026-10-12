## Entry 1349 — 2026-08-28 23:58 PDT — PB120 LANDED: WHEN-COMPILED's stamp is COMPILATION-scoped and the PE is deterministic — a long-lived compiler process gave every later compilation the FIRST one's timestamp

§15.99.3 r2 requires "the date and time of compilation of the compilation unit that contains this function";
the stamp was a process-static Lazy in IntrinsicRenderer, so the in-process battery (and any server-style host)
baked the first compilation's timestamp into every subsequent unit. Now ProgramEmitter captures it once per
EmitBound — through the SAME injectable IntrinsicBinder.CompileClock seam (D6) — and threads it via
EmitContext.WhenCompiledStamp, so a container and its contained units share one constant (r2's second sentence,
golden 2002/pb120_when_compiled_contained: SAME + the r1 shape checks) while each successive compilation gets
its own (unit test: two injected clocks, two compilations, each generated source carries its own Format21).

r3's object-code half is decided explicitly, not left to drift: RoslynBackend.Compile sets deterministic: true,
so the COFF TimeDateStamp is a content hash, the MVID is content-derived, and the generated object code provides
NO compilation date and time — r3's "if provided" condition is not engaged, and identical source + references
now produce a BYTE-IDENTICAL assembly (the determinism unit test failed pre-fix on the random MVID; my first
probe also failed post-fix until the output FILE NAMES matched — the module name embeds the output name).
Determination recorded in DESIGN-codegen-backend §1.7. Sweep: the only surviving process-static Lazy in the
compiler is the reference-assembly cache, correctly process-scoped. RV-15.99.3-2 → CONFORMS,
RV-15.99.3-3 → CONFORMS.


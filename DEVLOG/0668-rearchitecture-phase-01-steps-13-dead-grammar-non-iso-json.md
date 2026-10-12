## Entry 668 — 2026-07-07 19:45 PDT — Rearchitecture PHASE 01 steps 1–3 — dead-grammar + non-ISO JSON/XML removal

Began Phase 01 (mechanical rename + dead-grammar/JSON-XML removal) of the rearchitecture roadmap, straight off the P0
close-out (tree green). Three grammar-cleanup steps, each a green commit boundary.

**Step 1 — delete 5 dead top-level grammars.** `Grammar/{CobolParserJsonXml,CobolParserGenerics,CobolParserOO,
CobolDialect,CobolPreprocessor}.g4` — stale duplicates neither fed to ANTLR (only `Core/CobolLexer.g4` +
`CobolParserCore.g4` are) nor referenced by any C#. Verified: the only match outside build output was one inert comment
in `CobolExtensionsJsonXml.g4` (deleted in step 3). Forced clean regen + frontend build 0-err (−588 lines). `8b1236e`.

**Step 2 — remove committed `.antlr` IDE caches.** The 9 `Grammar/**/.antlr/*.{java,interp,tokens}` files (ANTLR
IDE-plugin java-target caches — build output) `git rm -r`'d + `**/.antlr/` gitignored. 0 tracked `.antlr` remain; build
0-err (−11,345 lines). `a261533`.

**Step 3 — hard-delete the non-ISO JSON/XML grammar.** `Core/CobolExtensionsJsonXml.g4` deleted; its `jsonStatement`/
`xmlStatement`/`jsonXmlExceptionPhrases` rules + the two `{is2014()}?` dispatch arms removed from `CobolParserCore.g4`
(import list too); the one genuinely-live 2023 rule `inlineMethodInvocationStatement` relocated verbatim into
`Core/CobolOO.g4`. JSON/XML are 0-occurrence in `specs/ISO_COBOL.md` (chair-verified, DEVLOG 586) — a hard invariant of
the rearchitecture. Lexer tokens (`JSON`/`XML`/`END_JSON`/`END_XML`) + the `cobolWord` words `PARSE`/`PROCESSING` are
retained (removing them is an out-of-scope reserved-word change). No C# referenced the removed contexts, so the whole
solution still builds.

**⚠ TRANSPARENCY — a phase-doc assumption was WRONG, caught by verify-by-running.** PHASE-01 step 3 asserted the guard
test `JsonGenerate_Below2014_VendorDisposition_Not0900` would still pass because `EditionGateHints` is "untouched and
still keys off `CobolLexer.JSON`". It did NOT: removing the `jsonStatement` arm changed the parse error from a
`NoViableAlternative` reported deep inside `procedureDivision` (where `EditionGateHints`' `InRule(ruleStack,
"procedureDivision")` guard matched) to an unexpected-token reported AFTER those frames unwind — the guard missed it and
the message regressed to a bare `COBOL0001: unexpected 'JSON'`. **Fix (same change set):** dropped the `procedureDivision`
guard from the JSON/XML branch of `EditionGateHints.Recognize`. Justification checked against the grammar: `JSON`/`XML`
are HARD-RESERVED tokens (absent from the `cobolWord` user-word set — only `PARSE`/`PROCESSING` are user words), so an
offending `JSON`/`XML` token is an unambiguous JSON/XML-statement signature on its own. Updated the test's stale doc
comment (≥2014 was formerly parse-then-runtime-loud; now parse-error at every edition — intended hardening) and the
PHASE-01 step-3 doc with the correction (process rule: keep design docs current + log the misstep). `EditionGateDiagnosticTests`
13/13 green.

Battery after step 3 (whole-solution build 0-err): greenfield conformance **2036/2036** + the FULL legacy guard (shared
front-end) **NIST 353 MATCH, 0 regressions** (legacy unit 1204 + integration 607, ALL GREEN) — the JSON/XML removal +
the `EditionGateHints` change regressed nothing. NEXT: P1 steps 4–5 (the `CobolSharp.Compiler.{Generated,Common,
Diagnostics,Parsing,Preprocessor}` → `CobolNet.Frontend.*` namespace rename), step 6 (Frontend banner + SLL-bail catch),
step 7 (csproj/design banners), step 8 (close-out).


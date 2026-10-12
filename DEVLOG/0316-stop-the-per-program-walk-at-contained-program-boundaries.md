## Entry 316 — Stop the per-program walk at contained-program boundaries → IC235A baselined (NIST 353→354)

**IC235A** was a COMPILE_FAIL: `CBL3107 Name 'PRINT-FILE' conflicts`, `CBL3101 Duplicate data-name 'PRINT-REC'
/ 'DUMMY-RECORD'`, `CBL3108 USING parameter 'ELEM-NON-01' not in LINKAGE SECTION`. Root cause (parallel
diagnosis): the three per-program tree-walk passes (`SemanticBuilder`, `ReferenceResolver`, `BoundTreeBuilder`)
have no `VisitNestedProgram` override, so ANTLR's default `VisitChildren` descended from a CONTAINING program
straight into its contained programs' subtrees and absorbed their declarations into the container's
scope/model. IC235A's container and its two contained programs (IC235A-1/-2) each declare `SELECT PRINT-FILE`
/ `FD PRINT-FILE` / `01 PRINT-REC` / `01 DUMMY-RECORD`; the contained ones, walked into the container, re-declare
the container's names (CBL3107/3101), and the contained `PROCEDURE DIVISION USING` params resolve against the
container's WORKING-STORAGE (CBL3108). Per ISO §8.4.6 a non-GLOBAL file/record/data name is LOCAL to its own
source element; `Compilation` already builds each contained program as its own element with its own model
(`CollectNestedPrograms`), so the container's walk must NOT enter them. (Latent until now: IC228A — the only
baselined genuinely-nested test — happens not to re-declare any container name.)

Fix: each of the three visitors captures the parse-tree node it was entered on (`_walkRoot`, via a `Visit`
override) and overrides `VisitNestedProgram` to descend only when the nested program IS the walk root (its own
build) and to skip it otherwise (`DefaultResult`) — so a containing program's walk treats each contained
program as an opaque boundary, while the contained program's OWN build still processes its body. GLOBAL
inheritance into a contained program is unaffected (it runs separately on the independently-built ancestor
models — IC228A: MATCH confirms it). IC235A → 012 OF 012, 0 FAIL*, baselined. Guard ALL GREEN: **1040 unit /
347 integration / 354 NIST** (IC 18→19), 0 regressions across the IC suite + the `CallTests` batch-compilation
cases.


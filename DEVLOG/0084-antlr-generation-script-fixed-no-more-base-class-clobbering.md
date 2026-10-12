## Entry 084 — 2026-03-16: ANTLR Generation Script Fixed — No More Base Class Clobbering

The ANTLR generation script now generates to a `Generated_temp/` folder, then copies only the ANTLR-generated files to `Generated/`, explicitly skipping `CobolParserCoreBase.cs` (hand-maintained in `Parsing/`). Clean target removes both `Generated/` and `Generated_temp/`.

MSBuild timing issue: when generated files don't exist, MSBuild's source file discovery happens before the generation target runs. This is a known MSBuild limitation with generated sources. Since generated files are committed to git, the practical workflow is: after a grammar change, run `pwsh Invoke-Antlr4CSharp.ps1` or build twice. First build generates files, second build compiles them.

---


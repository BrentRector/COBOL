## Entry 152 — 2026-03-26: Clean Build Fix + Test Un-Skips

**Problem:** `dotnet clean && dotnet build` failed. After `dotnet clean` deleted the Generated
folder, MSBuild's SDK-style source globbing ran before the ANTLR generation target, so `csc`
couldn't find `CobolParserCore` and ~200 other generated types.

**Root cause:** `BeforeTargets="BeforeBuild"` fires too late in the pipeline. The SDK glob
`**/*.cs` evaluates during project evaluation, before any targets run. Generated files
deleted by clean weren't present during globbing, so they were absent from the Compile
item group even after the generation target recreated them.

**Fix:** Changed to `BeforeTargets="CoreCompile"` and added an `<ItemGroup>` inside the
target that explicitly adds `Generated\*.cs` to `Compile` after generation. This ensures
files created by the target are included even when they were absent during initial globbing.

**Test un-skips:**
- `CallStatement_EmitsDiagnostic` → renamed `CallStatement_UnresolvedProgram_OnException`:
  CALL is fully implemented since Entry 142. Test now verifies ON EXCEPTION path for
  unresolved program name instead of checking for a diagnostic.
- `RefMod_ExpressionStartLength`: ref-mod with arithmetic expressions `FIELD(2 + 1:4 - 1)`
  now works via `BindSubscriptTokensAsArithmetic`. COBOL-85 §6.4.1 confirms arithmetic
  expressions are valid in ref-mod positions (unlike subscripts which are restricted §5.3).

**Results:** 216 unit + 183 integration pass, 1 skip (COBOL-2002 multiplication subscript).

---


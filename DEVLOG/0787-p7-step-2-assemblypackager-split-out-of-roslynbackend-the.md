## Entry 787 — 2026-07-11 14:26 PDT — P7 Step 2: `AssemblyPackager` split out of `RoslynBackend`; the ref cache was already P0's

**What (Step 2).** The side-effecting packaging moves out of `RoslynBackend` into the NEW
`CodeGen/AssemblyPackager.cs` (`Package(outputDllPath)` = runtimeconfig write + runtime-dll deploy;
`RuntimePath` lives there, consumed by both halves — the compile references it, the packager deploys it).
`RoslynBackend.Compile` is now PURE C#→assembly (its only writes: the assembly itself + the failed-emit
cleanup); `RoslynBackend.Emit` calls `AssemblyPackager.Package` on success. **Already-landed check:** the
`Lazy<ImmutableArray<MetadataReference>>` framework-reference cache the step prescribes was landed by P0 step 2
— nothing to do (verified in place). **Deviation (trivial, in the class doc):** `Package(string outputDllPath)`
instead of the sketch's `(EmitResult, BackendOptions)` — the output path is the one input packaging consumes.

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 32 characterization byte-exact.


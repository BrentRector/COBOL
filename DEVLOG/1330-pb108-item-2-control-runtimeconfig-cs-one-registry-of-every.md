## Entry 1330 — 2026-08-19 10:21 PDT — PB108 item 2: `Control/RuntimeConfig.cs` — ONE registry of every environment variable the runtime reads (nine names + the external switches' `COBOL_<SWITCH-NAME>` family), referencing the declaring constants, with a three-direction drift test fired once on an unregistered probe knob

**What landed.** `RuntimeConfig.All` / `Describe()` / `Find(name)` and `ConfigEntry` (name or family pattern,
subsystem, purpose, accepted values, declaring type + source file, `CurrentValue` read-only): `COBOL_USER_LOCALE`,
`COBOL_SYSTEM_LOCALE` (`LocaleState`), `COBOL_COLLATION_DIR` (`TailoringRules`), `COBOL_CLDR_DIR`
(`CldrLocaleLoader`), `COBOL_COLLATION_CACHE`, `COBOL_COLLATION_CACHE_EVICTION` (`CacheConfig`),
`COBOL_COLLATION_WARMUP` (`CollationRuntime`), `COBOLNET_CLOCK` (`SystemClock`), and the pattern entry
`COBOL_<SWITCH-NAME>` (`SwitchStore`). A diagnostic registry only — no new knob, nothing loaded or overridden, no
appsettings, no configuration framework; each subsystem reads its own constant exactly as before.

**Three behavior-neutral extractions made the constants exist where they did not:** `SwitchStore.Prefix` +
`SwitchStore.VariableNameFor(name)` (the concatenation was inline in `Get`), `SystemClock.PinVariable` (the
literal was inline in `Now()`), `TailoringRules.EnvDirectory` private → public. And one duplicate the literal scan
exposed on the first run: `LocaleConfig.TailoringDirectoryVariable` was a SECOND `"COBOL_COLLATION_DIR"` literal —
now `= TailoringRules.EnvDirectory` (one rule, one place), as its CLDR sibling already was.

**The drift test (`tests/Cobol.Net.Tests.Unit/RuntimeConfigTests.cs`, 6 facts) measures the runtime SOURCES, comments
stripped, in three directions:** every `Environment.GetEnvironmentVariable(` call site is in a file that declares a
registered entry (the registry's own generic read in `CurrentValue` is the one exempt site, asserted to be exactly
one); every identifier-shaped literal `"COBOL(NET)?_…"` is a registered name — and the bare `"COBOL_"` prefix is
spelled in `SwitchStore` only; every registered entry's file still spells its name and still reads the environment
(no stale entry); plus the binary complement — every `const string` of the runtime assembly whose value is such a
name is registered (`found >= registered` guards the scan itself). ⛔ The obvious test — grep for
`GetEnvironmentVariable("COBOL_` — finds zero sites, because every call passes a constant or a computed name; it
would have been vacuously green. The first run caught the test's own blind spot: the registry's doc comment MENTIONS
the call, and an un-stripped scan counted it as a second read — comments are stripped now. **Fired once:** a probe
`ProbeKnob` with an unregistered `COBOL_PROBE_UNREGISTERED` constant + read made all three drift facts red
(source read, literal, metadata), and green again when removed.

Wave-local: the 6 new facts + switches, clock seam, locale manager/state, tailoring, test-repo drift — 39/39. The
runtime README §7 documents the registry. Next: item 3 (`scripts/build-local.{sh,ps1}`), then T1.


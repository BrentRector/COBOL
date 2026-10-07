# `tests/Cobol.Net.Benchmarks/` — the BenchmarkDotNet harness

The measured performance profile of WiseOwl COBOL's performance-sensitive runtime subsystems: the **collation
engine** (`src/Cobol.Net.Runtime/Collation/`, kb/Work **PB101** — `Collation/CollationBenchmarks.cs`), the **key
cache** (**PB106** — `Collation/CacheBenchmarks.cs`), the **CLDR loader and builder** (**PB105** —
`Collation/CldrBenchmarks.cs`), and the **Unicode normalization and grapheme segmentation** subsystems
(**PB104** — `Unicode/UnicodeBenchmarks.cs`); and the **performance baseline**'s in-process half — compile throughput
and the generated programs' hot paths (**PB2117** — `Compile/`, `GeneratedCode/`, `Programs/`; see
[the baseline section](#the-performance-baseline--compile-throughput-and-generated-programs-kbwork-pb2117)).

This is **not a test project**. Nothing in CI runs it, it asserts nothing, and a red number here is a finding, not
a failure. It exists because the collation engine is on the hot path of ordinary COBOL — every relation condition
between alphanumeric operands under a LOCALE program collating sequence, every key comparison in `SORT`/`MERGE`,
every INDEXED-file key comparison, `MAX`/`MIN`, and `FUNCTION STANDARD-COMPARE` reach it
(`Collation/README.md` §6) — so a `SORT` of a million records is tens of millions of calls to
`Collator.Compare`. Its cost *and its allocation* per comparison are product characteristics.

It also puts a number on a design decision. `Collation/README.md` §1 declines to delegate to
`System.Globalization.CompareInfo` because the host's ICU floats with the operating system while a decade-old
INDEXED file must stay in key order. That is only defensible if the table-driven engine is in the same
performance class as the ICU it declines to use — so `CompareInfo` is a **baseline in every category here**,
alongside `string.CompareOrdinal` as the floor that no culturally-correct collation can reach.

---

## Running it

BenchmarkDotNet requires **Release**. A Debug build compiles and runs, but it measures a Debug JIT and the
harness says so; the numbers below are Release only. From the repository root:

```
dotnet build CobolSharp.sln -c Release
dotnet run -c Release --project tests/Cobol.Net.Benchmarks -- --filter *Collation*
```

Other useful invocations:

```
dotnet run -c Release --project tests/Cobol.Net.Benchmarks                     # every benchmark class
dotnet run -c Release --project tests/Cobol.Net.Benchmarks -- --list flat      # what exists
dotnet run -c Release --project tests/Cobol.Net.Benchmarks -- --job short      # ADDS a short job beside the configured one
dotnet run -c Release --project tests/Cobol.Net.Benchmarks -- --filter *BuildKey*
```

The whole class takes **about two minutes** (`CacheBenchmarks`, `CldrBenchmarks`, `UnicodeBenchmarks`: about two
minutes each; `--filter '*'` runs all four). Everything after `--` goes to BenchmarkDotNet, so its full command line is
available (`--help` lists it).

> **Quoting.** `*Collation*` must be quoted in a POSIX shell (`--filter '*Collation*'`) or the shell expands it
> against the working directory and BenchmarkDotNet reports `You must have made a typo in 'Collation'`.

The built executable can also be run directly, which skips `dotnet run`'s implicit rebuild:

```
tests/Cobol.Net.Benchmarks/bin/Release/net10.0/Cobol.Net.Benchmarks.exe --filter '*Collation*'
```

Results land in `BenchmarkDotNet.Artifacts/results/` beside the working directory — GitHub-flavoured Markdown,
HTML and CSV. That folder is a build output and is `.gitignore`d; the numbers worth keeping are the ones pasted
into this file.

**Configuration** is `Collation/BenchmarkConfig.cs`, applied by `[Config(typeof(BenchmarkConfig))]`: 3 warm-up +
12 measured iterations at ≥ 250 ms each, one launch, `MemoryDiagnoser` on, Median and P95 added to the default
columns, and logical grouping by `[BenchmarkCategory]` so the `Ratio` column always compares a benchmark against
the baseline **of its own category** — a 1.5 KB walk is never ratioed against a five-letter word.

---

## What each benchmark measures

Every corpus is built once in `[GlobalSetup]`, and the loops report **per comparison** (`OperationsPerInvoke`
equals the corpus size, checked against the corpus in `Setup` so an added pair cannot silently rescale the
report). Every method returns an accumulated `int` so nothing is dead-code-eliminated.

### `Compare-Short` — 64 ASCII pairs, the headline number

The corpus is eight groups of eight, each a distinct *shape* of the comparison, because averaging over the shapes
is what makes one number representative instead of a best case: difference at the first character (exits after
one collation element), in the middle, at the last character (a full level-1 walk), **case-only** (equal at
levels 1–2, decided at level 3 — three walks), **equal** (the worst case: every level walked and still tied),
**prefix** (one operand runs out), digits and record-key shapes, and **punctuation/spaces** — the variable
elements on which the root and Standard collators do different work.

| Benchmark | What it exercises |
|---|---|
| `Compare_ShortStrings` | `CollationEngine.Compare` — the CLDR root order (tertiary, non-ignorable). The number that multiplies by the record count in a `SORT`. |
| `Compare_ShortStrings_Standard` | `CollationEngine.Standard` — four levels, **shifted** variables: what `STANDARD-COMPARE`'s default ordering table `"ISO 14651_2020_TABLE1"` resolves to. Strictly more work. |
| `Compare_ShortStrings_Span` | `Collator.Compare(ReadOnlySpan<char>, ReadOnlySpan<char>)` — the overload the COBOL layer will call with slices of a record buffer, since trailing-space truncation re-slices rather than allocating a substring. |
| `Baseline_Ordinal_ShortStrings` | **Baseline.** `string.CompareOrdinal` — no collation at all; the floor. |
| `Baseline_IcuInvariant_ShortStrings` | The host's ICU, `CompareInfo.Compare(…, CompareOptions.None)`. |

### `Compare-Long` — 1.5 KB operands, the per-character cost

Two 1536-character ASCII strings sharing a 1535-character stem (no combining marks, so the comparison stays off
the NFD path and this measures the element walk itself).

| Benchmark | What it exercises |
|---|---|
| `Compare_LongStrings` | Differ at the **last character** → one full level-1 walk of both texts. Divide by 1536 for ns/character. |
| `Compare_LongStrings_CaseOnly` | Differ only in the **case** of the last character → levels 1, 2 and 3 all walked in full. Expected at ≈ 3× the above. |
| `Baseline_Ordinal_LongStrings` | **Baseline.** A vectorized `memcmp`. |
| `Baseline_IcuInvariant_LongStrings` | The host's ICU at the same length. |

### `Compare-Mixed` — off the ASCII fast path

Twelve pairs that reach the rest of the engine: accented Latin (a level-2 decision), the ß / æ / œ **expansions**
that turn one character into several collation elements, precomposed vs. decomposed text (which forces the
table-driven `Normalizer`, and is canonically **equal**, so every level is walked and still ties), Han (no table
entry at all — UTS #10 Table 16 implicit weights), Hangul (decomposed to conjoining jamo), and Spanish ñ.

| Benchmark | What it exercises |
|---|---|
| `Compare_MixedLocaleStrings` | The corpus under the root order. |
| `Compare_MixedLocaleStrings_SpanishTailored` | The same corpus through `CollationEngine.ForLocale("es-ES")`. A tailoring is a **new table**, not an indirection over the root one (`Collation/README.md` §5), so this should track the root closely. `"año"`/`"ano"` is the pair that moves: level 2 under the root order, level 1 under the tailoring. |
| `Baseline_Ordinal_MixedStrings` | **Baseline.** UTF-16 code-unit order. |
| `Baseline_IcuInvariant_MixedStrings` | The host's ICU — the fairest like-for-like in this file, since ICU also has real work to do here. |

### `BuildKey-Short` / `BuildKey-Long` — materializing a sort key

What an INDEXED file's key column stores, and what a `SORT` should use when one record is compared many times.
Unlike `Compare`, this **must** allocate: every level is materialized and nothing can exit early. The question
`MemoryDiagnoser` answers here is *how much, per key*.

| Benchmark | What it exercises |
|---|---|
| `BuildKey_ShortStrings` | `CollationKey.Build` over the 64 short operands. |
| `Baseline_IcuSortKey_ShortStrings` | **Baseline.** `CompareInfo.GetSortKey` — the direct ICU analogue. |
| `BuildKey_LongStrings` | A key for the 1.5 KB operand. |
| `Baseline_IcuSortKey_LongStrings` | **Baseline.** ICU's sort key at the same length. |

### What a regression looks like

- **`Allocated` becomes non-zero on `Compare_ShortStrings`, `Compare_ShortStrings_Span` or
  `Compare_LongStrings`.** `Collation/README.md` §4 says the common path is allocation-free. A non-zero cell on
  pure-ASCII input means something started buffering elements, materializing a key, or boxing — and a `SORT`
  would feel that as GC pressure long before it felt it as CPU. **This is the single most valuable column here.**
- **`Compare_LongStrings` growing faster than linearly in length.** The comparison is one forward pass per level;
  super-linear means a re-walk crept in.
- **`Compare_LongStrings_CaseOnly` drifting far from 3× `Compare_LongStrings`.** Much more means a level is doing
  redundant work; much less means a level is being *skipped*, which is a correctness bug the ordering tests in
  `Cobol.Net.Tests.Unit` should catch first.
- **`Compare_MixedLocaleStrings` jumping while the ASCII paths hold still.** That is the NFD / expansion /
  contraction / implicit-weight machinery.
- **`Compare_MixedLocaleStrings_SpanishTailored` separating from `Compare_MixedLocaleStrings`.** Tailored lookups
  would be taking a slower path than root ones, taxing every locale that has a tailoring at all.
- **The ratio against `CompareInfo` worsening by a large factor.** Across hosts the *ratio*, not the absolute
  nanoseconds, is the number to watch: losing an order of magnitude reopens §1's trade-off.

---

## Results

Two runs on **2026-08-18**, same host, `dotnet build CobolSharp.sln -c Release` followed by the filtered run above,
on an otherwise-idle developer workstation. **Run 1** is the engine as first landed (DEVLOG 1326); it found the
long-key outlier that became the identical-prefix skip (DEVLOG 1327). **Run 2** is the engine after that skip, with
the COBOL-layer carrier category added. Both pasted verbatim, including the host description BenchmarkDotNet prints.

### Run 2 — after the identical-prefix skip (the current engine)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900K 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.303
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  collation : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

| Method                                     | Categories     | Mean          | Error         | StdDev      | Median        | P95           | Ratio          | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |--------------- |--------------:|--------------:|------------:|--------------:|--------------:|---------------:|--------:|-------:|-------:|----------:|------------:|
| BuildKey_LongStrings                       | BuildKey-Long  | 30,278.226 ns | 1,205.1016 ns | 940.8640 ns | 29,930.518 ns | 31,471.505 ns |   2.16x slower |   0.08x | 1.9531 | 0.1221 |   37240 B | 11.41x more |
| Baseline_IcuSortKey_LongStrings            | BuildKey-Long  | 14,047.204 ns |   364.7035 ns | 284.7365 ns | 14,187.968 ns | 14,310.452 ns |       baseline |         | 0.1526 |      - |    3264 B |             |
| BuildKey_ShortStrings                      | BuildKey-Short |    182.663 ns |    11.4622 ns |   8.9489 ns |    186.685 ns |    191.842 ns |   1.11x slower |   0.06x | 0.0281 |      - |     530 B |  4.14x more |
| Baseline_IcuSortKey_ShortStrings           | BuildKey-Short |    164.312 ns |     3.5052 ns |   2.7366 ns |    164.281 ns |    168.095 ns |       baseline |         | 0.0067 |      - |     128 B |             |
| Compare_LongStrings                        | Compare-Long   |    696.036 ns |    11.7886 ns |   9.2038 ns |    695.269 ns |    709.333 ns |  13.53x slower |   0.34x |      - |      - |         - |          NA |
| Compare_LongStrings_CaseOnly               | Compare-Long   |    730.612 ns |    18.5817 ns |  14.5074 ns |    731.573 ns |    749.882 ns |  14.20x slower |   0.41x |      - |      - |         - |          NA |
| Baseline_Ordinal_LongStrings               | Compare-Long   |     51.478 ns |     1.5105 ns |   1.1793 ns |     51.531 ns |     53.131 ns |       baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_LongStrings          | Compare-Long   |    442.398 ns |    10.9080 ns |   8.5162 ns |    441.536 ns |    453.357 ns |   8.60x slower |   0.25x |      - |      - |         - |          NA |
| Compare_MixedLocaleStrings                 | Compare-Mixed  |    139.088 ns |     3.6994 ns |   2.8883 ns |    138.689 ns |    142.444 ns | 131.40x slower |   3.49x | 0.0048 |      - |      93 B |          NA |
| Compare_MixedLocaleStrings_SpanishTailored | Compare-Mixed  |    132.709 ns |     2.9481 ns |   2.3017 ns |    131.764 ns |    136.570 ns | 125.37x slower |   3.03x | 0.0048 |      - |      93 B |          NA |
| Baseline_Ordinal_MixedStrings              | Compare-Mixed  |      1.059 ns |     0.0249 ns |   0.0194 ns |      1.052 ns |      1.086 ns |       baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_MixedStrings         | Compare-Mixed  |     33.673 ns |     0.9114 ns |   0.6029 ns |     33.440 ns |     34.548 ns |  31.81x slower |   0.78x |      - |      - |         - |          NA |
| Compare_ShortStrings                       | Compare-Short  |     44.940 ns |     0.7422 ns |   0.5795 ns |     45.166 ns |     45.396 ns |  38.13x slower |   0.73x |      - |      - |         - |          NA |
| Compare_ShortStrings_Standard              | Compare-Short  |     95.451 ns |     2.0796 ns |   1.6236 ns |     94.936 ns |     97.684 ns |  80.99x slower |   1.78x |      - |      - |         - |          NA |
| Compare_ShortStrings_LocaleCollation       | Compare-Short  |     65.586 ns |     1.3984 ns |   1.0918 ns |     65.277 ns |     67.275 ns |  55.65x slower |   1.21x |      - |      - |         - |          NA |
| Compare_ShortStrings_Span                  | Compare-Short  |     44.637 ns |     1.1053 ns |   0.8629 ns |     44.543 ns |     45.612 ns |  37.87x slower |   0.90x |      - |      - |         - |          NA |
| Baseline_Ordinal_ShortStrings              | Compare-Short  |      1.179 ns |     0.0233 ns |   0.0182 ns |      1.174 ns |      1.205 ns |       baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_ShortStrings         | Compare-Short  |     13.131 ns |     0.3110 ns |   0.2428 ns |     13.153 ns |     13.453 ns |  11.14x slower |   0.26x |      - |      - |         - |          NA |
```

**What run 2 shows against run 1:** `Compare_LongStrings` **17,958 → 696 ns** (26×; now **1.6× the host ICU**, not
40×) and `Compare_LongStrings_CaseOnly` **54,038 → 731 ns** — the identical prefix is walked once with a vectorized
`CommonPrefixLength` instead of at every level; the two long variants are now within 5% of each other because the
level walks start at the last character. Short pairs improved too (**64.8 → 44.9 ns**, they share prefixes) — now
**3.4× ICU**. `Compare_ShortStrings_LocaleCollation` (the COBOL layer's carrier: §8.8.4.2.11 span trim, well-formedness
scan, per-use resolution of the run unit's locale, then the engine) is **65.6 ns — ~20 ns over the engine and 0 B
allocated**, so the carrier needs no devirtualized fast arm. Unchanged, by construction: STANDARD-COMPARE's shifted
lane (the skip is taken for state-free non-ignorable collators only), key building, and the mixed-script path's 93 B
(the NFD path — the next engine follow-up).

### Run 1 — the engine as first landed

```
// * Summary *

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900K 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.303
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  collation : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=collation  MinIterationTime=250ms  IterationCount=12  
LaunchCount=1  RunStrategy=Throughput  WarmupCount=3  

| Method                                     | Categories     | Mean          | Error         | StdDev        | Median        | P95           | Ratio            | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |--------------- |--------------:|--------------:|--------------:|--------------:|--------------:|-----------------:|--------:|-------:|-------:|----------:|------------:|
| BuildKey_LongStrings                       | BuildKey-Long  | 30,126.919 ns | 1,293.9620 ns | 1,010.2404 ns | 29,592.789 ns | 31,631.549 ns |     2.14x slower |   0.08x | 1.9531 | 0.1221 |   37240 B | 11.41x more |
| Baseline_IcuSortKey_LongStrings            | BuildKey-Long  | 14,112.770 ns |   323.9494 ns |   234.2367 ns | 14,189.182 ns | 14,340.230 ns |         baseline |         | 0.1526 |      - |    3264 B |             |
|                                            |                |               |               |               |               |               |                  |         |        |        |           |             |
| BuildKey_ShortStrings                      | BuildKey-Short |    175.915 ns |     6.2119 ns |     4.8499 ns |    175.001 ns |    184.289 ns |     1.09x slower |   0.04x | 0.0281 |      - |     530 B |  4.14x more |
| Baseline_IcuSortKey_ShortStrings           | BuildKey-Short |    161.978 ns |     4.4650 ns |     3.4860 ns |    163.692 ns |    164.956 ns |         baseline |         | 0.0067 |      - |     128 B |             |
|                                            |                |               |               |               |               |               |                  |         |        |        |           |             |
| Compare_LongStrings                        | Compare-Long   | 17,958.219 ns |   325.1138 ns |   253.8274 ns | 17,903.244 ns | 18,327.669 ns |   358.04x slower |  10.00x |      - |      - |         - |          NA |
| Compare_LongStrings_CaseOnly               | Compare-Long   | 54,038.279 ns | 1,244.8410 ns |   971.8900 ns | 53,840.997 ns | 55,356.270 ns | 1,077.39x slower |  32.21x |      - |      - |         - |          NA |
| Baseline_Ordinal_LongStrings               | Compare-Long   |     50.187 ns |     1.6432 ns |     1.2829 ns |     49.946 ns |     52.025 ns |         baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_LongStrings          | Compare-Long   |    446.190 ns |     9.1218 ns |     7.1217 ns |    449.688 ns |    452.384 ns |     8.90x slower |   0.26x |      - |      - |         - |          NA |
|                                            |                |               |               |               |               |               |                  |         |        |        |           |             |
| Compare_MixedLocaleStrings                 | Compare-Mixed  |    179.976 ns |     6.2988 ns |     4.9177 ns |    181.157 ns |    185.501 ns |   167.74x slower |   4.98x | 0.0045 |      - |      93 B |          NA |
| Compare_MixedLocaleStrings_SpanishTailored | Compare-Mixed  |    179.369 ns |     6.0076 ns |     4.6904 ns |    180.852 ns |    184.207 ns |   167.18x slower |   4.80x | 0.0045 |      - |      93 B |          NA |
| Baseline_Ordinal_MixedStrings              | Compare-Mixed  |      1.073 ns |     0.0258 ns |     0.0153 ns |      1.077 ns |      1.085 ns |         baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_MixedStrings         | Compare-Mixed  |     34.393 ns |     2.0579 ns |     1.4880 ns |     34.070 ns |     36.817 ns |    32.06x slower |   1.40x |      - |      - |         - |          NA |
|                                            |                |               |               |               |               |               |                  |         |        |        |           |             |
| Compare_ShortStrings                       | Compare-Short  |     64.788 ns |     1.7571 ns |     1.3719 ns |     65.391 ns |     66.051 ns |    54.90x slower |   1.69x |      - |      - |         - |          NA |
| Compare_ShortStrings_Standard              | Compare-Short  |     99.450 ns |     3.1013 ns |     2.4213 ns |     99.249 ns |    102.860 ns |    84.27x slower |   2.77x |      - |      - |         - |          NA |
| Compare_ShortStrings_Span                  | Compare-Short  |     68.594 ns |     2.2796 ns |     1.7797 ns |     68.136 ns |     71.283 ns |    58.12x slower |   1.98x |      - |      - |         - |          NA |
| Baseline_Ordinal_ShortStrings              | Compare-Short  |      1.181 ns |     0.0360 ns |     0.0281 ns |      1.182 ns |      1.214 ns |         baseline |         |      - |      - |         - |          NA |
| Baseline_IcuInvariant_ShortStrings         | Compare-Short  |     12.978 ns |     0.3423 ns |     0.2475 ns |     12.883 ns |     13.437 ns |    11.00x slower |   0.32x |      - |      - |         - |          NA |
```

The run reported no warnings and no config issues. Its only hint was outlier removal on four benchmarks
(1–3 outliers each, all on the baselines):

```
// * Hints *
Outliers
  CollationBenchmarks.Baseline_IcuSortKey_LongStrings: collation    -> 1 outlier  was  removed, 3 outliers were detected (13.70 us, 13.70 us, 14.65 us)
  CollationBenchmarks.Baseline_Ordinal_MixedStrings: collation      -> 3 outliers were removed (1.32 ns..1.64 ns)
  CollationBenchmarks.Baseline_IcuInvariant_MixedStrings: collation -> 1 outlier  was  removed (39.62 ns)
  CollationBenchmarks.Baseline_IcuInvariant_ShortStrings: collation -> 1 outlier  was  removed (13.82 ns)
```

### Reading the numbers

**The allocation claim holds.** Every comparison benchmark on ASCII input — short, Standard, span, both long
variants — reports `-` in `Allocated`: **zero bytes per comparison**, at every string length and under both
alternate-handling modes. `Collation/README.md` §4's "the common unequal-at-primary case is one pass and no
allocation" is now measured rather than asserted, and it is the property a million-record `SORT` depends on.

**The level model is confirmed to three digits.** `Compare_LongStrings_CaseOnly` / `Compare_LongStrings` =
54,038 / 17,958 = **3.01×**, against a prediction of exactly 3 (a case-only difference forces levels 1, 2 and 3 to
be walked in full where a last-character primary difference costs one walk). Nothing is being skipped and nothing
is walking twice.

**Against the host ICU.** On the corpus that matters most — short alphanumeric keys — the engine costs
**64.8 ns** per comparison against ICU's **13.0 ns**: **≈ 5.0× ICU**, at 55× ordinal. On the mixed-script corpus
the gap is similar (180.0 ns vs 34.4 ns, **≈ 5.2×**). Sort-key construction is where the engine is closest:
**176 ns vs 162 ns, 1.09×** for short operands. That is the same performance class, and it is the answer to §1's
trade-off: on this host, one order that does not float with the operating system costs about five times a
comparison that does.

**The exception is long strings**, and it is the one number worth following up. At 1536 characters the engine
takes **17.96 µs (11.7 ns/character)** where ICU takes **446 ns (0.29 ns/character)** — a gap of **40×**, against
5× on short input. That is not a constant factor stretched out; it is a different per-character regime. ICU almost certainly
has a vectorized Latin-1 fast path and a bulk common-prefix skip that the streaming
`CollationElementIterator` does not; the same shape shows in key construction (**2.14×** ICU at 1.5 KB against
**1.09×** at word length). COBOL alphanumeric keys are short, so this does not touch the common case — but a
`SORT` on a long `PIC X(2000)` key under a LOCALE collating sequence would feel it.

**Shifted costs about half as much again.** `Compare_ShortStrings_Standard` (four levels, variables shifted) is
**99.5 ns** against the root collator's **64.8 ns** — **1.54×**. That is the price of `STANDARD-COMPARE`'s
default ordering table over the CLDR/ICU default, on a corpus one-eighth of which is punctuation pairs that
genuinely need the fourth level.

**Tailoring is free.** `Compare_MixedLocaleStrings_SpanishTailored` (179.4 ns) and
`Compare_MixedLocaleStrings` (180.0 ns) are indistinguishable — 0.3% apart against a ±6 ns error. A tailored
table really is a table, not an indirection over the root one, so a locale with a tailoring pays nothing for
having one.

**The span and string overloads are indistinguishable.** 68.6 ns vs 64.8 ns, with the *string* overload — which
forwards to the span one and therefore cannot physically be faster — measuring lower. The ~4 ns is measurement
noise and code layout, not a real difference; treat the two as one number. The COBOL layer's re-slicing approach
to trailing-space truncation costs nothing.

**One allocation to look at.** `Compare_MixedLocaleStrings` allocates **93 B per comparison** averaged over the
twelve pairs — the only comparison path in the table that allocates at all. The candidate is the table-driven
`Normalizer`: two of the twenty-four strings in that corpus hold a combining mark and are decomposed to NFD
before the walk (`Collation/README.md` §4.1), and NFD returns a new string. If a COBOL program's data routinely
carries combining marks, that allocation is on its comparison path. Whether the whole 93 B is the normalizer, and
whether it can be a stack buffer for short inputs, is not answered by this harness — narrowing it is the obvious
next measurement.

---

## The other classes — cache, CLDR, Unicode (run 3, 2026-08-19)

Same host and configuration; `--filter '*CacheBenchmarks*' '*CldrBenchmarks*' '*UnicodeBenchmarks*'` (about six
minutes). Each category has the host's implementation of the same job as its baseline where one exists.

### `CacheBenchmarks` — the collation key cache (`Collation/Cache/README.md`)

| Category | Benchmark | Mean | Alloc | Reading |
|---|---|---|---|---|
| Cache-Hit | `Hit_ShortStrings` vs `Build_ShortStrings_NoCache` (baseline) | **23.6 ns** vs 214 ns (541 B) | 0 B | a hit is a dictionary lookup + interlocked stamp: **9× faster than building**, no allocation |
| Cache-Hit | `Hit_LongStrings` (400-char texts) | 82.7 ns | 0 B | the lookup hashes the text; still 2.6× the build |
| Cache-Miss | `Miss_ShortStrings` (a full 1,024-entry cache fed only new texts — every lookup misses, an eviction batch every 256 inserts) vs the disabled pass-through | 508 ns vs 355 ns | 770 B vs 664 B | steady-state churn costs **≈1.4× the bare build** — the eviction batch (a lock-free enumeration + sort by stamp) is amortized to ~150 ns per miss (a first version read `ConcurrentDictionary.Count` on every insert and `ToArray()`-snapshotted per batch — every bucket lock, both — and cost 27×; replaced by an interlocked count and the enumeration) |
| Cache-Compare-Short | `Compare_ShortStrings_ViaCache` vs the streaming `Collator.Compare` (baseline) | 48.0 ns vs **26.9 ns** | 0 B | **the streaming comparison wins for short operands** — two lookups cost more than deciding at the first differing primary; hence relation conditions never go through the cache |
| Cache-Compare-Long | 400-char texts sharing a 390-char stem, via cache vs streaming | 322 ns vs **246 ns** | 0 B | with an identical prefix to skip, streaming still wins |
| Cache-Compare-CaseDifferent | 120-char texts differing only in case throughout (equal through level 2, decided at level 3 — the record-comparison shape of a SORT), via cache vs streaming | **196 ns** vs 3,245 ns | 0 B | **16.6× faster through keys**: no prefix to skip, three full walks otherwise. This is the case SORT/MERGE and INDEXED keys are in, and why they key once and compare keys |

### `CldrBenchmarks` — the CLDR loader and tailoring builder (`Collation/CLDR/README.md`)

| Category | Benchmark | Mean | Alloc | Reading |
|---|---|---|---|---|
| CLDR-Parse | `Parse_Es` / `Parse_Da` / `Parse_Zh` | 5.7 µs / 5.0 µs / **18.7 ms** | 21 KB / 21 KB / 36 MB | LDML XML + the rule syntax; zh.xml is 1.1 MB (pinyin, stroke, zhuyin, unihan) |
| CLDR-Build | `Build_Es` / `Build_Da` / `Build_Vi` / `Build_Zh` | **6.1 ms / 6.5 ms / 7.7 ms / 50.5 ms** | 10.8 / 10.9 / 13.0 / 75.6 MB | rules → a tailored table + settings; the fixed ~6 ms is the table REBUILD (copying the 41,724-element pool and 38,787 mappings and renumbering the tertiary line, which almost every `<<<` forces); Vietnamese adds the closure over ~900 composites, Chinese the ~40,000 relations and a script reorder. **Paid once per process per locale** — see the next row |
| CLDR-Resolve | `Resolve_Cached_Es` / `Resolve_Cached_Zh` | **19.8 ns / 19.6 ns** | 40 B | what a program pays after the first use: the engine's per-tag cache lookup — identical for the lightest and the heaviest locale |

### `UnicodeBenchmarks` — normalization and segmentation (`Unicode/README.md`, `Unicode/Segmentation/README.md`)

| Category | Benchmark | Mean | Alloc | Reading |
|---|---|---|---|---|
| NFD-ASCII | `Nfd_Ascii` vs the host's `string.Normalize(FormD)` | 7.1 ns vs 1.1 ns | 0 B | our "needs NFD?" scan of a 32-char text vs the host's ASCII short-circuit; both return the input by reference |
| NFD-Accented | `Nfd_Accented` (accented / composed / Hangul corpus) vs the host | 50.3 ns vs 39.4 ns | **394 B** vs 32 B | the table-driven decomposition is in the host's class in time and behind it in allocation — the `List<int>` + string build in `Normalizer.ToNfd`; the next engine follow-up (also the 93 B on the mixed-script comparison path) |
| NFC | `Nfc_Accented` (the subsystem) vs the host directly | 32.9 ns vs 32.0 ns | 2 B | the subsystem's NFC IS the host's composer behind the invariant-mode fallback: 1 ns of dispatch |
| Segment-ASCII | `Segment_Ascii` (`GraphemeBreaker.Count`, 32-char texts) vs the host's `StringInfo.LengthInTextElements` | **129 ns vs 450 ns** | 0 B vs 192 B | **3.5× faster than the host, no allocation** — one property lookup per code point |
| Segment-Mixed | marks, emoji sequences, flags, Hangul jamo, Indic conjuncts | **11.3 ns vs 39.2 ns** | 0 B vs 66 B | 3.5× |
| Segment-Long | enumerating a ~2 KB mixed text vs `StringInfo.GetTextElementEnumerator` | **7.5 µs vs 24.9 µs** | 0 B vs 37 KB | 3.3× |

The raw BenchmarkDotNet tables of run 3 are in the session's `BenchmarkDotNet.Artifacts/results/` (not committed);
the numbers above are copied from them.

---

## The performance baseline — compile throughput and generated programs (kb/Work PB2117)

Two classes are the in-process half of the baseline that `scripts/arch/perf_baseline.py` records and compares
(kb/Work A6's instrument; `docs/rearchitecture/DESIGN-architecture-review.md` §3 R0 and §4 item 5). Run them through
the script, which also takes the cold, whole-process and GnuCOBOL measurements and writes the record to
`docs/rearchitecture/evidence/perf-baseline/<product commit>.md`; the numbers live there, not here.

- **`Compile/CompileBenchmarks`** — `CompilerDriver.Compile` over the three largest NIST programs (IX113A, NC105A,
  NC218A) and the one making the most COPY statements (SM201A), compiled as the NIST harness compiles them.
  `FrontEndAndBind` (the `CheckOnly` compile: preprocess, parse, bind, the conformance pass) is the baseline and
  `FullCompile` adds emit, Roslyn and the written assembly, so the `Ratio` column is the backend's share. Witness: both
  compiles succeed, the assembly is non-empty, and SM201A really read its library texts.
- **`GeneratedCode/GeneratedProgramBenchmarks`** — the programs under `Programs/` (`MOVEHOT` moves through every storage
  form, `PERFHOT` is PERFORM dispatch, `SEQHOT` sequential and `IDXHOT` indexed file I/O), compiled once at the shipping
  edition and run in this process through their entry point: the generated code and the runtime, without process
  start-up. Witness: the program's whole output must equal its line in `Programs/witnesses.tsv`, which is computed from
  the formula in the program's header comment, never copied from a run. `Programs/EMPTYRUN.cob` is the start-up floor
  the script's whole-process rows are read against.

The `Programs/` sources are plain COBOL 85 on purpose: the script runs the same files through GnuCOBOL 3.2 under WSL,
so the comparison always measures the same COBOL. Each has exactly one `01 LOOP-COUNT PIC 9(9) COMP VALUE n.` line,
which the script scales (1x, 2x, 4x) for the scaling curve.

---

## Not measured here

**The COBOL layer beyond the carrier.** `Compare_ShortStrings_LocaleCollation` (run 2) measures the
`CobolCollation` carrier's LOCALE arm; the table arms (`AlphanumericCollation` / `NationalCollation`) and SORT/MERGE end
to end are not benchmarked yet (indexed-file key handling is, end to end, by `IDXHOT`).

**Numeric conversion in isolation.** `MOVEHOT` measures `MOVE` across the storage forms as a program does it; there is
no micro-benchmark of one conversion. Add one as a sibling folder (`tests/Cobol.Net.Benchmarks/<Subsystem>/`) reusing
`Collation/BenchmarkConfig.cs`, which is deliberately subsystem-neutral (`Unicode/`, `Compile/` and `GeneratedCode/`
already do).

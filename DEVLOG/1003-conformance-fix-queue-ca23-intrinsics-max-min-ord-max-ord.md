## Entry 1003 — 2026-07-23 01:23 PDT — Conformance fix-queue: CA23 (intrinsics) — MAX/MIN/ORD-MAX/ORD-MIN honor the PROGRAM COLLATING SEQUENCE (+ §8.8.4.2.7 r2 space-padding)

Third intrinsics fix — the collation one. §15.59.4 r1 etc.: MAX/MIN/ORD-MAX/ORD-MIN determine the greatest/least by
§8.8.4.2 relation-condition rules, which use the current alphanumeric PROGRAM COLLATING SEQUENCE (§8.8.4.2.7) or the
national PCS (§8.8.4.2.9). The runtime compared with `string.CompareOrdinal` (raw UTF-16), so under a non-default PCS
MAX/MIN DISAGREED with the program's own relation conditions (which DO honor the PCS via CobolString.Compare), and it
never space-padded unequal-length operands (§8.8.4.2.7 r2).

**Fix (runtime + binder + renderer, one seam).** (1) Runtime `CobolIntrinsics.Exact.cs`: the 4 methods now funnel
through ONE `ExtremeIndex(xs, max, cmp)` helper (singular pattern), comparing via `CobolString.Compare` — the default
parameterless body pad-compares (identical to CompareOrdinal for equal-length, so no golden churn; corrects unequal
length) and two new overloads per method take `ushort[] weights` / `NationalCollation` for a non-identity PCS. (2)
Binder `IntrinsicBinder`: the existing `collate`/`collateNat` flags now also fire for the MAX/MIN string form (gated on
`resolved.RuntimeMethod`, so numeric MAX is untouched) — alphanumeric-PCS+alphanumeric-args → `collate`, national-PCS+
national-args → `collateNat`. (3) Renderer `IntrinsicRenderer`: a new `CollatePrefix(ic)` emits `__COLLATE, ` /
`__COLLATE_NAT, ` FIRST (a `params string[]` can take no trailing arg — the mirror of CHAR/ORD's trailing `Collate`),
selecting the weighted/national overload. Default PCS emits the parameterless call unchanged (byte-stable).

**Verified (CLI):** under `ALPHABET AL IS "ZYX…CBA"` + `PROGRAM COLLATING SEQUENCE AL` (Z lowest … A highest),
`FUNCTION MAX(A Z)` → "A" and `IF A > Z` → A-GT-Z — MAX now AGREES with the relation (was "Z", contradicting it).
Golden `tests/conformance/2002/intrinsic_max_collating`.

**Gate (wave-local FILTERED):** characterization 33/33 (default-PCS emit byte-identical), intrinsic+collating+alphabet+
national conformance 222/222, CorpusRunner 322/322 (new golden byte-matches), intrinsic+collating unit 7/7.


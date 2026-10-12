## Entry 377 — M2-DATA-2 follow-up: `USAGE FLOAT-EXTENDED` (ISO §13.18, COBOL-2002)

Closes the FLOAT-EXTENDED follow-up Entry 376 flagged. The standard's extended floating-point usage is
implementor-defined "widest available" precision; .NET has no native 128-bit float, so it maps to IEEE-754
double (= COMP-2) — the platform's widest native float, a documented approximation rather than a rejection.
Same three-edit alias shape as FLOAT-SHORT/LONG:
- lexer token `FLOAT-EXTENDED` (hyphenated section, after `FLOAT-LONG`);
- `usageKeyword` + bare-form `usageClause` gain `FLOAT_EXTENDED`;
- `UsageMapper.FromUsageKeyword` maps `"FLOAT-EXTENDED" => Comp2`.

Verified `01 WS-X USAGE FLOAT-EXTENDED` with `250.5 * 2` = 501. The `float_usage` conformance test now covers
all three FLOAT-* usages (S=0021 / L=0401 / X=0501). No precision claim beyond double is made — programs
that genuinely require >64-bit floats are out of scope until a soft-float quad path exists (noted in plan).

Guard ALL GREEN: **1047 unit / 469 integration / 364 NIST**, 0 regressions.


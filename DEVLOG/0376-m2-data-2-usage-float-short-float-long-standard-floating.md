## Entry 376 — M2-DATA-2: `USAGE FLOAT-SHORT` / `FLOAT-LONG` standard floating point (ISO §13.18)

Plan Wave 2 item. The COBOL-2002 standard floating-point usages are exact aliases of the existing IEEE-754
COMP-1/COMP-2: `FLOAT-SHORT` = single (= COMP-1), `FLOAT-LONG` = double (= COMP-2). A clean three-edit alias:
- lexer tokens `FLOAT-SHORT` / `FLOAT-LONG`;
- `usageKeyword` (and the bare-form `usageClause` list) gain both;
- `UsageMapper.FromUsageKeyword` maps `"FLOAT-SHORT" => Comp1`, `"FLOAT-LONG" => Comp2`, so they inherit COMP-1/2's
  no-PICTURE handling, storage width (4/8 bytes), and arithmetic unchanged.

Verified: `01 WS-S USAGE FLOAT-SHORT` with `10.5 * 2` = 21 and `01 WS-L FLOAT-LONG` with `100.25 * 4` = 401.
Tests: `FloatShortAndLong_AliasComp1And2` + conformance `tests/conformance/2002/float_usage`. (FLOAT-EXTENDED —
128-bit — remains a follow-up; .NET has no native quad.)

Guard ALL GREEN: **1047 unit / 469 integration (+2) / 364 NIST**, 0 regressions.


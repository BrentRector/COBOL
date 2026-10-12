## Entry 066 — 2026-03-15: COMP/BINARY Decode + Encode

Added DecodeCompBinary and EncodeCompBinary for USAGE COMP/BINARY fields:
- 2/4/8-byte signed big-endian integer encoding
- Respects FractionDigits and P scaling
- Two's complement signed representation
- Wired into DecodeNumeric/EncodeNumeric switch (previously fell through to
  DecodeDisplay which treated binary bytes as ASCII text)

Updated DecodeNumeric: `UsageKind.Comp or UsageKind.Binary => DecodeCompBinary`
Updated EncodeNumeric: added Comp/Binary case calling EncodeCompBinary

NC101A: still 51/89. F1-6/F1-11/F1-12 still FAIL (need further investigation —
may be multi-target MULTIPLY or ON SIZE ERROR issues, not just COMP decoding).

Footer still shows "NO TEST(S) FAILED" despite visible FAIL results.
Investigation shows counters are PIC 999 DISPLAY (not COMP), so the COMP
fix doesn't help. The FAIL paragraph does `ADD 1 TO ERROR-COUNTER` which
should increment, but ERROR-COUNTER remains 0 — suggesting ADD to DISPLAY
numeric is silently failing to accumulate.

---

---


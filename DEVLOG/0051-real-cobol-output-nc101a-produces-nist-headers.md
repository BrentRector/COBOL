## Entry 051 — 2026-03-14: REAL COBOL OUTPUT — NC101A Produces NIST Headers

### The Breakthrough

NC101A now produces actual NIST-formatted print output:
```
OFFICIAL COBOL COMPILER VALIDATION SYSTEM
CCVS85 4.2  COPY - NOT FOR DISTRIBUTION
TEST RESULT OF NC101A    IN  HIGH        LEVEL VALIDATION FOR ...
FOR OFFICIAL USE ONLY            COBOL 85 VERSION 4.2, Apr  1993 SSVG
FEATURE              PASS  PARAGRAPH-NAME                    REMARKS
TESTED               FAIL
```

This required fixing three fundamental things:

1. **Hierarchical DataSymbol tree**: SemanticBuilder uses a level-number stack to
   build proper parent/child trees. FILLER gets unique internal names. All items
   preserved in declaration order.

2. **Recursive storage layout**: Groups share their children's bytes. Elementary
   items allocate bytes and advance the offset. Group offset = first child's offset,
   group size = span of all children.

3. **VALUE clause initialization**: .cctor writes initial values into the correct
   byte positions. String and numeric literals handled. Figurative constants
   (SPACE, ZERO) normalized.

The chain that produces output:
- .cctor: VALUE "OFFICIAL COBOL..." → bytes at offset 50 in WorkingStorage
- MOVE CCVS-H-1 TO DUMMY-RECORD → copies 120 bytes from group start
- WRITE DUMMY-RECORD → outputs those 120 bytes as ASCII to print-file.txt

150 lines written. Headers, column labels, and page breaks all present.

---


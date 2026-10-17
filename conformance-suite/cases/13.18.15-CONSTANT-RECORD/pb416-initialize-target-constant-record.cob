*>--- conformance-case
*> id: pb416-initialize-target-constant-record
*> rules: [SR-13.18.15.3-2, SR-14.9.20.3-7]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB416 — ISO 14.9.20.3 SR7: "The data item referenced by identifier-1 is the receiving operand."
*> That is not a designation, it is a CONSEQUENCE: identifier-1 is thereby subject to every receiving-operand
*> prohibition the standard states, and 13.18.15.3 SR2 is one — "Neither the data item described by the
*> subject of the entry nor any data item subordinate to the subject of the entry shall be specified as a
*> receiving data item."
*> The CONSTANT RECORD clause is COBOL-2002 (13.18.15; D5), so 85 is not a reject edition for this program —
*> below 2002 it is refused for the clause itself (COBOLNET0900), which is a different rule.
*> Before PB416, INITIALIZE resolved identifier-1 with the plain reference resolver instead of the ONE
*> receiving chokepoint, so COBOLNET1548 — the diagnostic the MOVE form of the same store already got — never
*> fired and the structured constant was DESTROYED at run time, silently. identifier-1 now resolves through
*> ExpressionBinder.ResolveReceiving, which is a funnel rather than a check: the CAPACITY register, a
*> constant-name, LINE-COUNTER and PAGE-COUNTER are refused by the same routing, and so is the next
*> receiving-operand prohibition added there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB416NCR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CR CONSTANT RECORD.
          05 CN PIC 9(4) VALUE 7.
          05 CA PIC X(5) VALUE "abcde".
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE CR.
           STOP RUN.

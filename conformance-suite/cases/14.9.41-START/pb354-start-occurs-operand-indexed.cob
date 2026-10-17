*>--- conformance-case
*> id: pb354-start-occurs-operand-indexed
*> rules: [SR-14.9.41.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.41.3 SR4 - "Data-name-1 or record-key-name-1 shall not be subject to any OCCURS
*> clauses." The indexed arm used to reject this only as a SIDE EFFECT of the offset walk bailing out
*> on an OCCURS ancestor, and it reported SR6's message - a sentence that is FALSE of IX-ELEM (1),
*> which DOES begin at the record key's leftmost character position and IS shorter than it. SR4 now
*> has its own named check ahead of the position walk, so the reported reason is the real one.
IDENTIFICATION DIVISION.
PROGRAM-ID. P354OCIX.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p354ocix.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY.
      10 IX-ELEM PIC X(3) OCCURS 2 TIMES.
   05 IX-DATA PIC X(10).
PROCEDURE DIVISION.
MAIN.
    START IXF KEY IS = IX-ELEM (1) END-START
    STOP RUN.

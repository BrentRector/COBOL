*>--- conformance-case
*> id: pb354-start-operand-too-long
*> rules: [SR-14.9.41.3-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.41.3 SR6 b) 3. - "Its length is not greater than the length of that record key."
*> IX-LONG begins at the prime key's leftmost character position within a record of the file and has the
*> same class, category and usage, so b) 1. and b) 2. are both satisfied and b) 3. is the ONLY condition
*> left to reject it: nine character positions against a six-character key. Its accept side is
*> conformance:cases/12.4.5.12-RECORD-KEY/pb354_start_generic_key_ok K3 (a 3-character generic key over the same X(6) key), so
*> the threshold is pinned from both directions.
IDENTIFICATION DIVISION.
PROGRAM-ID. P354LONG.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p354long.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY  PIC X(6).
   05 IX-DATA PIC X(10).
01 IX-REC2.
   05 IX-LONG PIC X(9).
   05 IX-LTL  PIC X(7).
PROCEDURE DIVISION.
MAIN.
    START IXF KEY IS = IX-LONG END-START
    STOP RUN.

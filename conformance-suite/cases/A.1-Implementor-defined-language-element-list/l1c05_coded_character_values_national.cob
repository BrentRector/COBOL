      *>--- conformance-case
      *> id: l1c05_coded_character_values_national
      *> rules: [DOC-A.1-32]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.1.2 (A.1 item 32) — the national coded character values,
      *>   and the editing E
      *> Rule (A.1 item 32): "Computer's coded character set (coded
      *>   character values for certain COBOL items). This item is
      *>   required. This item shall be documented in the implementor's
      *>   user documentation."
      *> cite.py --check 8.1.2 "The implementor shall specify one and
      *>   only one national coded character value associated with each
      *>   of the following COBOL elements" -> OK  §8.1.2 3)
      *>   (Computer's coded character set)
      *> cite.py --check 8.1.2 "The implementor shall specify one and
      *>   only one alphanumeric coded character value" -> OK  §8.1.2 3)
      *>   (Computer's coded character set)
      *> cite.py --check 15.70.4 "If the class of argument-1 is
      *>   national, the returned value is the ordinal position of
      *>   argument-1 in the current national program collating
      *>   sequence" -> OK  §15.70.4 2)  (Returned value rules)
      *> The documented choice pinned (docs/CONFORMANCE.md DOC-A.1-32):
      *>   the national values are the SAME code units as the
      *>   alphanumeric ones
      *>   (conformance:85/l1c05_coded_character_values), and the
      *>   floating-point editing E is U+0045. ORD = code unit + 1 in
      *>   both native sets (docs/CONFORMANCE.md DOC-A.1-8).
      *> Derivation (ORD, decimal):
      *>   national SPACE 0020h 033; QUOTE 0022h 035; ZERO 0030h 049
      *>   national space fill (N"A" into N(3), position 2) 033
      *>   PIC 9B09 USAGE NATIONAL of 12 = "1 02": B 033, the inserted 0
      *>     049
      *>   PIC 99CR USAGE NATIONAL of -12 = "12CR": C 068, R 083
      *>   alphanumeric +9.9E+99 of 15 = "+1.5E+01": E 45h 070
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05O.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-NC   PIC N.
       01 W-C    PIC X.
       01 W-L    PIC X(6).
       01 W-O    PIC 9(3).
       01 W-NF   PIC N(3).
       01 W-NEG  PIC S99 VALUE -12.
       01 N-B0   PIC 9B09 USAGE NATIONAL.
       01 N-CR   PIC 99CR USAGE NATIONAL.
       01 E-FLT  PIC +9.9E+99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE SPACE TO W-NC. MOVE "NSPACE" TO W-L. PERFORM SHOW-N.
           MOVE QUOTE TO W-NC. MOVE "NQUOTE" TO W-L. PERFORM SHOW-N.
           MOVE ZERO TO W-NC.  MOVE "NZERO" TO W-L.  PERFORM SHOW-N.
           MOVE N"A" TO W-NF.
           MOVE W-NF(2:1) TO W-NC. MOVE "NFILL" TO W-L. PERFORM SHOW-N.
           MOVE 12 TO N-B0.
           MOVE N-B0(2:1) TO W-NC. MOVE "NB" TO W-L. PERFORM SHOW-N.
           MOVE N-B0(3:1) TO W-NC. MOVE "N0" TO W-L. PERFORM SHOW-N.
           MOVE W-NEG TO N-CR.
           MOVE N-CR(3:1) TO W-NC. MOVE "NC" TO W-L. PERFORM SHOW-N.
           MOVE N-CR(4:1) TO W-NC. MOVE "NR" TO W-L. PERFORM SHOW-N.
           MOVE 15 TO E-FLT.
           MOVE E-FLT(5:1) TO W-C. MOVE "E" TO W-L.
           MOVE FUNCTION ORD(W-C) TO W-O.
           DISPLAY W-L "=" W-O.
           STOP RUN.
       SHOW-N.
           MOVE FUNCTION ORD(W-NC) TO W-O.
           DISPLAY W-L "=" W-O.

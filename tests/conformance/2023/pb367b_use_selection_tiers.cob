      *> kb/Work PB367b - THE 14.9.49.4 GR3 SELECTION TIERS, AND TIER ORDER BEATING SOURCE ORDER.
      *> GR3: "A declarative is selected for execution by analyzing the USE statements in the source element in
      *> the order in which they are specified. The first declarative that satisfies the selection criteria is
      *> executed and no other declaratives are executed. ... Otherwise, the following rules are applied in
      *> order: ... c) All format 3 USE statements in which file-name-2 is specified and exception-name-2 is a
      *> level-3 exception-name are examined. ... d) ... file-name-2 is specified and exception-name-2 is a
      *> level-2 exception-name ... e) ... file-name-2 is not specified and exception-name-1 is a level-3
      *> exception-name ... f) ... file-name-2 is not specified and exception-name-1 is a level-2 exception-name
      *> ... g) Any format 3 USE statements in which file-name-2 is not specified and exception-name-1 is a
      *> level-1 exception-name ...". Each tier ends "If no qualifying USE statement is found, the USE
      *> statements in the source element are examined AGAIN" - so the tiers are a cascade over the WHOLE
      *> declarative set, and a later-declared declarative in an earlier TIER beats an earlier-declared one in a
      *> later tier. The six declaratives below are written in the reverse of the tier order precisely so that
      *> source order cannot produce these answers.
      *>
      *> EXPECTED LINES, DERIVED FROM THE RULES:
      *>  . 14.6.13.1.6 Table 13 / 14.6.13.1.1 - EC-I-O is a level-2 name, EC-I-O-AT-END a level-3 name under
      *>    it; EC-BOUND is level-2 with EC-BOUND-OVERFLOW under it; EC-ALL is the single level-1 name.
      *>  . READ F1 past the end: 9.1.13 gives I-O status 10 and, with checking on, EC-I-O-AT-END is raised
      *>    (9.1.13.1). Tier c) finds D-C (EC-I-O-AT-END FILE F1) - the LAST declarative in source order =>
      *>    TIER-C. 14.9.49.4 GR12 b) then returns control after the READ (status 10 is not fatal).
      *>  . READ F2 past the end: no tier-c candidate names F2, so the cascade re-examines and tier d) finds
      *>    D-D (EC-I-O FILE F2) => TIER-D.
      *>  . READ F3 past the end: no file-scoped candidate names F3; tier e) finds D-E (EC-I-O-AT-END) =>
      *>    TIER-E.
      *>  . MOVE 22 TO WS-E (5): 8.5.1.9.6 GR1 raises the nonfatal EC-BOUND-OVERFLOW on the first implicit
      *>    crossing of the expected capacity 4. No declarative names that level-3 name, so tiers c/d/e find
      *>    nothing and tier f) finds D-F (EC-BOUND) => TIER-F. The declarative completes normally and the
      *>    growth proceeds, so WS-E (5) holds 22 (this is also the row that proves the RUNTIME-site raise
      *>    reaches the tiers at all - kb/Work PB367b).
      *>  . IF INV-RANGE: 14.7.8 rule 2 raises the nonfatal EC-RANGE-INVALID for a THROUGH range whose
      *>    starting value collates after its ending value. Nothing names EC-RANGE-INVALID or EC-RANGE, so the
      *>    cascade reaches tier g), where BOTH D-G1 and D-G2 qualify: GR3's "in the order in which they are
      *>    specified" and "the first declarative that satisfies the selection criteria is executed and no
      *>    other declaratives are executed" select D-G1 => TIER-G1 and no TIER-G2 line. Execution then
      *>    proceeds as if the range were empty (rule 2), so the ELSE branch runs.
      *>  . 14.9.49.3 SR14 bounds the duplication that is illegal - "The same pair of exception-name-2 and
      *>    file-name-2 shall not be specified in more than one USE statement within the same procedure
      *>    division" - and says nothing about exception-name-1, which is why two EC-ALL declaratives are legal
      *>    source and why GR3 has to say which of them wins.
      >>TURN EC-I-O-AT-END CHECKING ON
      >>TURN EC-BOUND-OVERFLOW CHECKING ON
      >>TURN EC-RANGE-INVALID CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB367BTIER.
       ENVIRONMENT DIVISION.
      *> INV-RANGE is ordered IN a LOCALE alphabet: §13.18.63.3 SR26 refuses an inverted range whose runtime
      *> collating sequence is known (COBOLNET2961, kb/Work PB552), and a locale's is not.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET LOC IS LOCALE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb367b-tier1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb367b-tier2.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F3 ASSIGN TO "pb367b-tier3.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(3).
       FD F2.
       01 F2-REC PIC X(3).
       FD F3.
       01 F3-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 4.
       01 WS-C  PIC X VALUE "M".
          88 INV-RANGE VALUE "Z" THRU "A" IN LOC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-G1 SECTION.
           USE AFTER EC EC-ALL.
       D-G1-P.
           DISPLAY "TIER-G1".
       D-G2 SECTION.
           USE AFTER EC EC-ALL.
       D-G2-P.
           DISPLAY "TIER-G2".
       D-F SECTION.
           USE AFTER EC EC-BOUND.
       D-F-P.
           DISPLAY "TIER-F".
       D-E SECTION.
           USE AFTER EC EC-I-O-AT-END.
       D-E-P.
           DISPLAY "TIER-E".
       D-D SECTION.
           USE AFTER EC EC-I-O FILE F2.
       D-D-P.
           DISPLAY "TIER-D".
       D-C SECTION.
           USE AFTER EC EC-I-O-AT-END FILE F1.
       D-C-P.
           DISPLAY "TIER-C".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F1 F2 F3.
           WRITE F1-REC FROM "AAA".
           WRITE F2-REC FROM "BBB".
           WRITE F3-REC FROM "CCC".
           CLOSE F1 F2 F3.
           OPEN INPUT F1 F2 F3.
           READ F1.
           READ F1.
           DISPLAY "AFTER-F1".
           READ F2.
           READ F2.
           DISPLAY "AFTER-F2".
           READ F3.
           READ F3.
           DISPLAY "AFTER-F3".
           CLOSE F1 F2 F3.
           MOVE 22 TO WS-E (5).
           DISPLAY "AFTER-BO=" WS-E (5).
           IF INV-RANGE DISPLAY "RI-TRUE" ELSE DISPLAY "RI-FALSE" END-IF.
           STOP RUN.

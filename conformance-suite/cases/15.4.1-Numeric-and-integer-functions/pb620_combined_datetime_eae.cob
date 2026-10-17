*>--- conformance-case
*> id: pb620_combined_datetime_eae
*> rules: [RV-15.4.1-1, AR-15.17.3-2, RV-15.17.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB620 — ISO §15.17 COMBINED-DATETIME under a STANDARD arithmetic mode: the function EQUALS
      *> its own equivalent arithmetic expression whatever CARRIER argument-2 arrives on.
      *>
      *> THE RULE. §15.17.4 r1: "The equivalent arithmetic expression is as follows:" argument-1 +
      *> (argument-2 / 100000). §15.4.1, for a standard mode: "the returned value shall equal the value of
      *> the equivalent arithmetic expression", and its NOTE 2 states the consequence outright — the relation
      *> condition `function-identifier = equivalent-arithmetic-expression` "will evaluate to true". The
      *> argument's own rule places NO fraction-digit limit on it: §15.17.3 r2 says only "Argument-2 shall be
      *> in standard numeric time form", and §15.5.5 defines that form by MAGNITUDE alone — "a numeric value
      *> representing seconds past midnight", >= 0 and < 86,400 (< 86,401 under >>LEAP-SECOND ON).
      *>
      *> THE DEFECT. COMBINED-DATETIME was the one §15.4.1 r1 function still consuming its argument through
      *> the LANDING intake, which truncates an SDIDI operand at max(receiver scale, 6). A plain data-item
      *> argument is not on that carrier and was always exact; a §15.3 type-10 ARITHMETIC EXPRESSION is, and
      *> in a receiver-less context — a relation condition, a DISPLAY — it lost every digit past the sixth.
      *> Measured before the fix: the EQ leg below printed BAD, and DISPLAY of the same reference printed
      *> 1.03661123456 against the 1.0366112345678 its literal-argument twin printed in the same program.
      *>
      *> THE EXPECTED VALUES are computed from the rule, not observed: 3661.12345678 / 100000 =
      *> 0.0366112345678 exactly (a division by a power of ten is a decimal point shift, so it is exact on
      *> both carriers at every argument scale), + 1 = 1.0366112345678, which a PIC 9V9(13) receiver holds
      *> with no truncation at all. A and B are one value reaching one receiver over the two
      *> carriers; C is the equivalent arithmetic expression spelled out by hand.
      *>
      *> ARMED, NOT ASSUMED. G1/G2 are the §15.17.3 r2 screen, asked of both carriers: 86400 seconds is
      *> outside standard numeric time form under the implied >>LEAP-SECOND OFF (§7.3.17.4 GR5, "greater than
      *> or equal to zero and less than 86,400"), so §15.3 sets EC-ARGUMENT-FUNCTION, the declarative reports
      *> it, and the abandoned COMPUTE leaves the receiver at its preset. Both carriers ask ONE screen, so a
      *> guard that drifted between them would show here as one ARMED line instead of two.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB620CDT.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S   PIC 9(5)V9(8) VALUE 3661.12345678.
       01 WS-BAD PIC 9(5)V9(8) VALUE 86400.
       01 WS-A   PIC 9V9(13).
       01 WS-B   PIC 9V9(13).
       01 WS-C   PIC 9V9(13).
       01 ES     PIC X(20).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           MOVE FUNCTION EXCEPTION-STATUS TO ES
           DISPLAY "ARMED=" ES.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> A — the EXACT carrier: a plain data item, at its own scale.
           COMPUTE WS-A = FUNCTION COMBINED-DATETIME(1, WS-S)
           DISPLAY "A=" WS-A
      *> B — the SDIDI carrier: the same VALUE through a §15.3 type-10 arithmetic expression.
           COMPUTE WS-B = FUNCTION COMBINED-DATETIME(1, WS-S + 0)
           DISPLAY "B=" WS-B
      *> C — §15.17.4 r1's equivalent arithmetic expression, written out.
           COMPUTE WS-C = 1 + ((WS-S + 0) / 100000)
           DISPLAY "C=" WS-C
      *> EQ — §15.4.1 NOTE 2's relation condition, in a receiver-less context: the leg the landing broke.
           IF FUNCTION COMBINED-DATETIME(1, WS-S + 0) = 1 + ((WS-S + 0) / 100000)
               DISPLAY "EQ=OK"
           ELSE
               DISPLAY "EQ=BAD"
           END-IF
      *> AGREE — §15.4.1 requires the returned value to be "the same for all instances of a given
      *> function ... so long as the value and order of the arguments ... are unchanged". The CARRIER an
      *> argument arrives on is not part of that proviso, so the two references are one value.
           IF FUNCTION COMBINED-DATETIME(1, WS-S) =
              FUNCTION COMBINED-DATETIME(1, WS-S + 0)
               DISPLAY "AGREE=OK"
           ELSE
               DISPLAY "AGREE=BAD"
           END-IF
      *> G1/G2 — the §15.17.3 r2 screen from each carrier in turn.
           MOVE 9 TO WS-A
           COMPUTE WS-A = FUNCTION COMBINED-DATETIME(1, WS-BAD)
           DISPLAY "G1=" WS-A
           MOVE 9 TO WS-B
           COMPUTE WS-B = FUNCTION COMBINED-DATETIME(1, WS-BAD + 0)
           DISPLAY "G2=" WS-B
           STOP RUN.
       END PROGRAM PB620CDT.

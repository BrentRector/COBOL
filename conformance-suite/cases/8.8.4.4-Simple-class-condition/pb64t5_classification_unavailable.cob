*>--- conformance-case
*> id: pb64t5_classification_unavailable
*> rules: [GR-8.8.4.4.4-3, GR-14.6.6-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.2.1 — "If the locale is not found during an operation requiring a locale, the EC-LOCALE-MISSING exception
      *> condition is set to exist and the operation is unsuccessful" — for a CHARACTER CLASSIFICATION (12.3.6.4 GR5 a)
      *> naming a DECLARED locale no environment provides: DETERMINATION L1 makes availability a RUN-TIME property (the
      *> compiler never resolves an external identification), so the program compiles and the condition is raised AT USE
      *> by the operations that require the classification locale — the class tests (GR7 b) and the case functions
      *> without a LOCALE phrase (GR7 a) — through the ONE 8.2.1 gate (LocaleFacts.Require). Table 13 (14.6.13.1.6) makes
      *> EC-LOCALE-MISSING FATAL; with checking ON the USE declarative observes it and RESUME AT NEXT STATEMENT continues;
      *> with checking OFF the coded character set's behavior stands (14.6.13.1.3 #8 — the implementor's determination,
      *> CONFORMANCE.md 4 item 5). Increment T5 of docs/rearchitecture/DESIGN-locale-facility.md (kb/Work PB64).
      *>
      *> What each line proves:
      *>   OFF-ALPHA / OFF-UP — checking off: "ab cd" IS ALPHABETIC (the coded character set's 8.8.4.4.4 GR3 b2 set, space
      *>                        included) and UPPER-CASE("i") → "I" (ORD 74) — the implementor's correspondence (15.97.4 r4)
      *>                        stands, and nothing is raised.
      *>   HANDLED / ON-ALPHA — after >>TURN EC-LOCALE-MISSING CHECKING ON the class test raises; the IF statement is
      *>                        interrupted (14.6.13.1.3 #5 — the declarative runs, RESUME AT NEXT STATEMENT), so VERDICT
      *>                        keeps its initial "?" — displayed to prove the statement did not silently complete.
      *>   HANDLED / ON-UP    — the case function raises too; the MOVE is interrupted and S keeps "????".
      *> Every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T5MISS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS XX.
       SPECIAL-NAMES.
           LOCALE XX IS "xx-NOWHERE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SPACED          PIC X(5) VALUE "ab cd".
       01  S               PIC X(4) VALUE "????".
       01  N               PIC 9(5).
       01  VERDICT         PIC X VALUE "?".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-MISSING.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           IF SPACED IS ALPHABETIC DISPLAY "OFF-ALPHA=yes" ELSE DISPLAY "OFF-ALPHA=no" END-IF
           MOVE FUNCTION UPPER-CASE("i") TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "OFF-UP=" N
           MOVE "????" TO S
       >>TURN EC-LOCALE-MISSING CHECKING ON
           IF SPACED IS ALPHABETIC MOVE "y" TO VERDICT ELSE MOVE "n" TO VERDICT END-IF
           DISPLAY "ON-ALPHA=" VERDICT
           MOVE FUNCTION UPPER-CASE("i") TO S
           DISPLAY "ON-UP=" S
           STOP RUN.

*>--- conformance-case
*> id: pb101_ec_order_not_supported
*> rules: [GR-12.3.7.4-17, RV-15.85.4-2, DOC-A.1-42]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.85.4 rule 2: "If the cultural ordering table is not available on the processor, or the specified
      *> ordering level is not available, or the level number specified by argument-4 is not defined in the
      *> ordering table, the EC-ORDER-NOT-SUPPORTED exception condition is set to exist." Table 13 (14.6.13.1.6)
      *> makes it FATAL, so 14.6.13.1.3 #5 runs the applicable USE declarative and RESUME AT NEXT STATEMENT is
      *> what keeps the run unit alive.
      *>
      *> ⛔ THE POINT OF THIS GOLDEN IS THAT THE CONDITION IS *OBSERVED*, not merely registered. A registered
      *> exception-name with no raise site is a zero-fan-out result that reads as coverage (the standing rule in
      *> DESIGN-locale-facility 4.10). Two things are pinned: the raise itself, and the fact that the NAME is
      *> legal again at every naming site — >>TURN and USE both take it. It was refused BY NAME with COBOLNET1518
      *> until support for ISO/IEC 14651:2020 was claimed under Annex A.3 item 25 (kb/Work PB100 -> PB101 T7).
      *>
      *> 12.3.7.4 GR17 leaves the allowable content of literal-9 to the implementor, so "NO SUCH TABLE" is LEGAL
      *> source that this processor cannot resolve: the compile succeeds with the COBOLNET1662 advisory and the
      *> reference raises at run time. That is the whole point of the warning rather than an error.
      *>
      *> 15.85.4 r6 gives the returned value; with the condition raised and the declarative resuming at the next
      *> statement, the MOVE never completes, so R keeps its VALUE — displayed to prove the statement was
      *> interrupted rather than silently completed with a default.
       >>TURN EC-ORDER-NOT-SUPPORTED CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB101ECORD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ORDER TABLE BADT IS "NO SUCH TABLE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X VALUE "?".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ORDER-NOT-SUPPORTED.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE FUNCTION STANDARD-COMPARE("a" "b" BADT) TO R.
           DISPLAY "R=[" R "]".
           DISPLAY "AFTER".
           STOP RUN.

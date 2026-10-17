*>--- conformance-case
*> id: pb332_use_ec_after_optional
*> rules: [FMT-14.9.49.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> !! AFTER IS AN OPTIONAL WORD OF USE FORMAT 3 TOO (kb/Work PB332).
      *> ISO 14.9.49.2 Format 3 prints `USE AFTER { EXCEPTION CONDITION | EC } ...` with USE,
      *> EXCEPTION, CONDITION, EC and FILE UNDERLINED and AFTER NOT underlined - measured off the
      *> PDF's vector rectangles and confirmed on the 600 dpi render of printed folio 774; the
      *> transcription's own figure notes for Formats 3 and 4 record the same measurement. 5.2.3
      *> makes a non-underlined uppercase word an OPTIONAL word and 8.3.2.4.3 says it "may be
      *> specified at the user's option with no effect on the semantics of the format". Both
      *> declaratives below therefore omit AFTER; both were COBOL0001 until PB332.
      *> 14.9.49.3 SR12: "EC is synonymous with EXCEPTION CONDITION", so H2 exercises the short
      *> form of the same format with the same word omitted.
      *> DERIVATION - each expected line follows from the rules, nothing from the compiler.
      *>  . 8.4.3.3.4: a reference modification whose leftmost position exceeds the item's length
      *>    sets EC-BOUND-REF-MOD; WS-X is 5 positions and the reference is (7:2). Table 13 makes it
      *>    fatal, so under >>TURN ... CHECKING ON the declarative is selected (14.9.49.4 GR6 c)).
      *>  . 8.4.2.3.4 GR2: a subscript "greater than the highest permissible occurrence number" sets
      *>    EC-BOUND-SUBSCRIPT; T has 3 occurrences and IDX is 5.
      *>  . 15.33: FUNCTION EXCEPTION-STATUS returns the name of the exception condition that exists,
      *>    so each handler prints the EC name that selected it - which is what proves the AFTER-less
      *>    spelling bound to the right declarative rather than merely parsing.
      *>  . 14.9.33.4 GR2 a): RESUME AT NEXT STATEMENT returns control to the statement following the
      *>    one that raised the condition, so both AFTER- lines are reached and neither MOVE stored.
      >>TURN EC-BOUND-REF-MOD CHECKING ON
      >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB332UEC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(5) VALUE "HELLO".
       01 WS-Y PIC X(2) VALUE "??".
       01 G.
          05 T PIC 9(2) OCCURS 3 TIMES.
       01 IDX PIC 9(2) VALUE 5.
       01 R  PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H1 SECTION.
           USE EXCEPTION CONDITION EC-BOUND-REF-MOD.
       H1-P.
           DISPLAY "H1=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       H2 SECTION.
           USE EC EC-BOUND-SUBSCRIPT.
       H2-P.
           DISPLAY "H2=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE WS-X (7:2) TO WS-Y.
           DISPLAY "AFTER-1=[" WS-Y "]".
           MOVE T (IDX) TO R.
           DISPLAY "AFTER-2=[" R "]".
           STOP RUN.

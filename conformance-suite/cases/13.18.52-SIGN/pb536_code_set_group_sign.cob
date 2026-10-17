      *>--- conformance-case
      *> id: pb536_code_set_group_sign
      *> rules: [SR-13.18.52.3-3, GR-13.18.52.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB536 - the CODE-SET record screen vs a SIGN clause INHERITED from a containing group.
      *>
      *> ISO 13.18.13.3 SR3 a): "if alphabet-name-1 is specified, all elementary data items of all record
      *> description entries associated with the file shall be described as usage display, and any signed
      *> numeric data items shall be described with the SIGN IS SEPARATE clause". 13.18.52.3 SR3 states the
      *> same requirement from the SIGN clause's side.
      *>
      *> WHAT "DESCRIBED WITH" MEANS FOR A SUBORDINATE ITEM is 13.18.52.4 GR1: "The SIGN clause specifies the
      *> position and the mode of representation of the operational sign for the numeric item to which it
      *> applies, OR FOR EACH NUMERIC ITEM SUBORDINATE TO THE GROUP TO WHICH IT APPLIES." N is subordinate to
      *> G, G carries SIGN IS LEADING SEPARATE, and N's picture character-string contains 'S' (GR1 sentence 2),
      *> so the clause applies to N: this record IS described with the SIGN IS SEPARATE clause and the program
      *> SHALL COMPILE. The compiler used to refuse it with COBOLNET1672 because the screen ran at file-section
      *> bind time, one phase before the group-clause propagation - a rejection by an ordering, not by a rule.
      *>
      *> EVERY expected character below is computed from the rules, never read off a run:
      *>   13.18.52.4 GR6 a) - with SEPARATE CHARACTER "the operational sign is presumed to be the leading
      *>     (or, respectively, trailing) character position of the data item to which it applies; this
      *>     character position is not a digit position", and GR6 b) - "The operational signs for positive and
      *>     negative are the basic special characters '+' and '-', respectively". LEADING, so N is one sign
      *>     character followed by its four digit positions: 5 character positions.
      *>   14.9.25.4 GR5 / 14.6.8 - MOVE -12 TO a S9(4) receiver aligns on the decimal point and space-fills,
      *>     which for a numeric receiver is zero fill: the digit positions hold 0012. With the negative sign
      *>     of GR6 b) that is the five characters -0012.
      *>   The record R is those 5 positions followed by T's 2 - the 7 characters -0012ZZ - and 13.18.13.4 GR7
      *>     makes STANDARD-1's medium correspondence the identity on these characters, so the round trip
      *>     through the file is byte-exact and R reads back unchanged.
      *>
      *> The other direction - a signed numeric record item under a CODE-SET FD that is described with NO
      *> SIGN SEPARATE clause at all, neither its own nor an inherited one - is
      *> conformance:negative/pb536-code-set-signed-without-separate (COBOLNET1672).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB536GS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL1 IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb536gs.dat"
           ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F CODE-SET IS AL1.
       01  R.
           05  G SIGN IS LEADING SEPARATE.
               10  N PIC S9(4).
           05  T PIC X(2).
       WORKING-STORAGE SECTION.
       01  DONE PIC X VALUE "N".
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           MOVE -12 TO N
           MOVE "ZZ" TO T
           WRITE R
           CLOSE F
           MOVE SPACE TO T
           OPEN INPUT F
           READ F AT END MOVE "Y" TO DONE END-READ
           DISPLAY "N=[" N "]"
           DISPLAY "R=[" R "]"
           DISPLAY "DONE=" DONE
           CLOSE F
           STOP RUN.

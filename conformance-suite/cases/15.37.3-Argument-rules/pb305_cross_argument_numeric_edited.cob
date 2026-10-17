      *>--- conformance-case
      *> id: pb305_cross_argument_numeric_edited
      *> rules: [AR-15.37.3-2, AR-15.48.3-3, AR-15.59.3-2, AR-15.63.3-2, AR-15.68.3-2, AR-15.71.3-3, AR-15.72.3-3, AR-15.79.3-3, AR-15.87.3-2, AR-15.92.3-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB305 - A NUMERIC-EDITED OPERAND STANDING BESIDE AN
      *> ALPHANUMERIC ONE AT A CROSS-ARGUMENT CLASS RULE.
      *>
      *> ISO 8.5.2.1 Table 2 puts category numeric-edited (usage
      *> display) in
      *> class ALPHANUMERIC, and 8.4.3.3.4 GR6 c) says it in words: "the
      *> categories numeric and numeric-edited are considered class and
      *> category national if the usage is national; otherwise they are
      *> considered class and category alphanumeric". EVERY
      *> cross-argument
      *> rule in 15 is CLASS-worded - 15.37.3 r2 and 15.96.3 r2 say
      *> "either
      *> class alphabetic or alphanumeric", 15.87.3 r2 says it per
      *> argument-2/argument-3 pair, 15.59.3 r2 / 15.63.3 r2 / 15.71.3
      *> r3 /
      *> 15.72.3 r3 say "All arguments shall be of the same class", and
      *> 15.48.3 r3 / 15.79.3 r3 / 15.92.3 r2's "the same type as
      *> argument-1" is the class reading - so a numeric-edited operand
      *> and
      *> an alphanumeric one agree at every one of them.
      *>
      *> EACH LINE IS AN EQUIVALENCE, NOT A PINNED LITERAL, and that is
      *> deliberate: the assertion is exactly what Table 2 states - the
      *> numeric-edited item and the BYTE-IDENTICAL PIC X twin beside it
      *> are
      *> of one class, so every class-worded rule must return the same
      *> answer for both. It needs no knowledge of the runtime collating
      *> sequence (which MAX/MIN/ORD-MAX/ORD-MIN would otherwise leak
      *> in)
      *> and it fails if EITHER spelling drifts. The twin is built by
      *> MOVE
      *> from the edited item, so byte-identity is established by the
      *> program rather than asserted by its author.
      *>
      *> Each line ALSO PRINTS the shared value, so the assertion cannot
      *> pass VACUOUSLY - two equally broken results would still print
      *> "OK",
      *> and the printed value is what makes that visible. Three are
      *> anchored to a value derived from the standard without reference
      *> to
      *> the collating sequence:
      *>   FS-ARG2  15.37.4 r1 - "the character position of the first
      *>            occurrence where the string represented by
      *>            argument-2
      *>            matches a substring within argument-1": H is
      *>            "AB  5CDEFG" and ED holds the three characters "  5"
      *>            (MOVE 5 to a PIC ZZ9 suppresses both leading zeros),
      *>            which first matches at character position 3.
      *>   SFT      15.79.4 r1 - "((H * 3600) + (M * 60) + S)":
      *>            12*3600 + 34*60 + 56 = 45296.
      *>   TFD 15.92.4 r1 - "If no format problems or range problems
      *>            occur ... the value returned is zero": "20210616" is
      *>            a
      *>            valid YYYYMMDD date, so 0.
      *>
      *> THE SHAPE THAT WAS MISSING IS THE MIXED PAIRING.
      *> find_string_argument1_classes already carried a numeric-edited
      *> FIND-STRING case - FIND-STRING(EDH EDN) - but BOTH its
      *> arguments
      *> were numeric-edited, so the two candidate sets intersected
      *> non-empty and the defect could not show. Every pairing here is
      *> mixed, and each is written in BOTH orders where r2 admits both.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB305CROSS23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> The edited operands and their byte-identical alphanumeric
      *> twins.
       01 ED   PIC ZZ9.
       01 EDX  PIC X(3).
       01 E1   PIC Z.
       01 E1X  PIC X.
       01 EDT  PIC ZZZZZZZ9.
       01 EDTX PIC X(8).
       01 EDS  PIC ZZZZZ9.
       01 EDSX PIC X(6).
       01 H    PIC X(10) VALUE "AB  5CDEFG".
       01 T    PIC X(6)  VALUE "55AB55".
       01 P1   PIC 9(4).
       01 P2   PIC 9(4).
       01 S1   PIC X(12).
       01 S2   PIC X(12).
       01 N1   PIC S9(9).
       01 N2   PIC S9(9).
       PROCEDURE DIVISION.
           MOVE 5 TO ED.
           MOVE ED TO EDX.
           MOVE 5 TO E1.
           MOVE E1 TO E1X.
           MOVE 20210616 TO EDT.
           MOVE EDT TO EDTX.
           MOVE 123456 TO EDS.
           MOVE EDS TO EDSX.
           DISPLAY "ED=[" EDX "] E1=[" E1X "] EDT=[" EDTX "]".

      *> 15.37.3 r2 - the edited item as argument-2, then as argument-1.
           MOVE FUNCTION FIND-STRING(H ED) TO P1.
           MOVE FUNCTION FIND-STRING(H EDX) TO P2.
           IF P1 = P2
             DISPLAY "FS-ARG2=OK " P1
           ELSE
             DISPLAY "FS-ARG2=BAD " P1 " " P2
           END-IF.
           MOVE FUNCTION FIND-STRING(ED "5") TO P1.
           MOVE FUNCTION FIND-STRING(EDX "5") TO P2.
           IF P1 = P2
             DISPLAY "FS-ARG1=OK " P1
           ELSE
             DISPLAY "FS-ARG1=BAD " P1 " " P2
           END-IF.

      *> 15.96.3 r2 - argument-2 is "a single character", so the edited
      *> twin at that position is a one-position PIC Z.
           MOVE FUNCTION TRIM(T E1) TO S1.
           MOVE FUNCTION TRIM(T E1X) TO S2.
           IF S1 = S2
             DISPLAY "TRIM-ARG2=OK [" S1 "]"
           ELSE
             DISPLAY "TRIM-ARG2=BAD [" S1 "] [" S2 "]"
           END-IF.
           MOVE FUNCTION TRIM(ED "5") TO S1.
           MOVE FUNCTION TRIM(EDX "5") TO S2.
           IF S1 = S2
             DISPLAY "TRIM-ARG1=OK [" S1 "]"
           ELSE
             DISPLAY "TRIM-ARG1=BAD [" S1 "] [" S2 "]"
           END-IF.

      *> 15.87.3 r2 - the argument-2/argument-3 pair against argument-1.
           MOVE FUNCTION SUBSTITUTE(ED "5" "X") TO S1.
           MOVE FUNCTION SUBSTITUTE(EDX "5" "X") TO S2.
           IF S1 = S2
             DISPLAY "SUB-ARG1=OK [" S1 "]"
           ELSE
             DISPLAY "SUB-ARG1=BAD [" S1 "] [" S2 "]"
           END-IF.
           MOVE FUNCTION SUBSTITUTE(H E1 "X") TO S1.
           MOVE FUNCTION SUBSTITUTE(H E1X "X") TO S2.
           IF S1 = S2
             DISPLAY "SUB-ARG2=OK [" S1 "]"
           ELSE
             DISPLAY "SUB-ARG2=BAD [" S1 "] [" S2 "]"
           END-IF.

      *> 15.59.3 r2 / 15.63.3 r2 / 15.71.3 r3 / 15.72.3 r3 - the
      *> AllSameClass arm, which the note did not name and which the
      *> pre-fix screen refused just as widely.
           MOVE FUNCTION MAX(ED "999") TO S1.
           MOVE FUNCTION MAX(EDX "999") TO S2.
           IF S1 = S2
             DISPLAY "MAX=OK [" S1 "]"
           ELSE
             DISPLAY "MAX=BAD [" S1 "] [" S2 "]"
           END-IF.
           MOVE FUNCTION MIN(ED "999") TO S1.
           MOVE FUNCTION MIN(EDX "999") TO S2.
           IF S1 = S2
             DISPLAY "MIN=OK [" S1 "]"
           ELSE
             DISPLAY "MIN=BAD [" S1 "] [" S2 "]"
           END-IF.
           MOVE FUNCTION ORD-MAX(ED "999") TO P1.
           MOVE FUNCTION ORD-MAX(EDX "999") TO P2.
           IF P1 = P2
             DISPLAY "ORDMAX=OK " P1
           ELSE
             DISPLAY "ORDMAX=BAD " P1 " " P2
           END-IF.
           MOVE FUNCTION ORD-MIN(ED "999") TO P1.
           MOVE FUNCTION ORD-MIN(EDX "999") TO P2.
           IF P1 = P2
             DISPLAY "ORDMIN=OK " P1
           ELSE
             DISPLAY "ORDMIN=BAD " P1 " " P2
           END-IF.

      *> 15.48.3 r3 / 15.92.3 r2 / 15.79.3 r3 - the date/time family's
      *> "the same type as argument-1", read as the class.
           COMPUTE N1 =
             FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" EDT).
           COMPUTE N2 =
             FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" EDTX).
           IF N1 = N2
             DISPLAY "IFD=OK " N1
           ELSE
             DISPLAY "IFD=BAD " N1 " " N2
           END-IF.
           COMPUTE N1 =
             FUNCTION TEST-FORMATTED-DATETIME("YYYYMMDD" EDT).
           COMPUTE N2 =
             FUNCTION TEST-FORMATTED-DATETIME("YYYYMMDD" EDTX).
           IF N1 = N2
             DISPLAY "TFD=OK " N1
           ELSE
             DISPLAY "TFD=BAD " N1 " " N2
           END-IF.
           COMPUTE N1 =
             FUNCTION SECONDS-FROM-FORMATTED-TIME("hhmmss" EDS).
           COMPUTE N2 =
             FUNCTION SECONDS-FROM-FORMATTED-TIME("hhmmss" EDSX).
           IF N1 = N2
             DISPLAY "SFT=OK " N1
           ELSE
             DISPLAY "SFT=BAD " N1 " " N2
           END-IF.
           STOP RUN.

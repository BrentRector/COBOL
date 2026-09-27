      *> kb/Work PB1380 - the REF-MOD-ZERO-LENGTH directive (ISO 7.3.23)
      *> over a reference-modified level-66 RENAMES entry, in all three
      *> states. 7.3.23.3 GR1: "When this directive is omitted or is
      *> specified as off, then when reference-modification results in a
      *> zero-length data item, the exception condition EC-BOUND-REF-MOD
      *> is raised"; 8.4.3.3.4 GR5c: "However, when the REF-MOD-ZERO-LENGTH
      *> directive is in effect, a zero-length result is allowed." A
      *> THROUGH alias is an alphanumeric group item (13.18.45.4 GR2) and
      *> a no-THROUGH alias takes data-name-2's attributes (GR1), so both
      *> are identifier-1 of 8.4.3.3.3 SR1 like any other item. Before the
      *> fix the reference modifier on an alias was DROPPED: no leg below
      *> raised, and Z2 overwrote the whole span with "Z".
      *> Omitted:  Z1 RN(1:N) sender, Z2 RN(2:N) receiver, Z3 RA(1:N)
      *>           sender each raise (N = 0); the declarative reports it
      *>           and RESUME AT NEXT STATEMENT leaves T and G unchanged.
      *>           Z4 RN(2:3) is non-zero and raises nothing -> GGH.
      *> ON:       Z5 RN(2:N) receiver and Z6 RN(1:N) sender raise
      *>           nothing; Z5 stores into no position (G unchanged) and
      *>           Z6's zero-length sender space-fills T.
      *> OFF:      Z7 S(1:N) and Z8 RN(1:N) raise again.
      >>TURN EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1380-RENAMES-ZL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 S PIC X(5) VALUE "ABCDE".
       01 T PIC X(6) VALUE "------".
       01 G.
          05 G1 PIC X(3) VALUE "GGG".
          05 G2 PIC X(3) VALUE "HHH".
       66 RN RENAMES G1 THRU G2.
       66 RA RENAMES G2.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       H-P.
           DISPLAY "  CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "Z1".
           MOVE RN (1:N) TO T.
           DISPLAY "  T=[" T "]".
           DISPLAY "Z2".
           MOVE "Z" TO RN (2:N).
           DISPLAY "  G=[" G "]".
           DISPLAY "Z3".
           MOVE RA (1:N) TO T.
           DISPLAY "  T=[" T "]".
           DISPLAY "Z4".
           MOVE RN (2:3) TO T.
           DISPLAY "  T=[" T "]".
       >>REF-MOD-ZERO-LENGTH ON
           DISPLAY "Z5".
           MOVE "Z" TO RN (2:N).
           DISPLAY "  G=[" G "]".
           DISPLAY "Z6".
           MOVE RN (1:N) TO T.
           DISPLAY "  T=[" T "]".
       >>REF-MOD-ZERO-LENGTH OFF
           DISPLAY "Z7".
           MOVE S (1:N) TO T.
           DISPLAY "Z8".
           MOVE RN (1:N) TO T.
           STOP RUN.

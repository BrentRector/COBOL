      *> kb/Work PB2671 - an OMITTED formal read with no checking
      *> enabled reads its CATEGORY-DEFAULT value, the value the same
      *> formal holds unbound at the main-program entry, whatever its
      *> category, usage or storage (a carrier, or an AREA because it
      *> is addressed, a group or floating-point).
      *> Rules (cite.py OK):
      *>  OK 14.9.4.4 11) the omitted-argument condition is true
      *>  OK 14.9.4.4 12) "If a parameter for which the
      *>     omitted-argument condition is true is referenced in a
      *>     called program, except as an argument or in the
      *>     omitted-argument condition, the EC-PROGRAM-ARG-OMITTED
      *>     exception condition is set to exist" - checking is not
      *>     enabled here, so the reference reads what the
      *>     implementation documents.
      *> Documented determination (docs/CONFORMANCE.md DOC-A.1-141,
      *> omission of parameters; DOC-A.1-116): the reference reads
      *> the category default - spaces for an alphanumeric, national
      *> or edited item, zeros for a boolean item, zero for a
      *> numeric item of any usage, a group's from its elementary
      *> items.
      *> Derivation: every PB2671OA formal is ADDRESS OF-referenced
      *> or is a group or floating-point item (an AREA formal); in
      *> PB2671OB only L5 and L6 are areas and the rest are
      *> carriers. Each line names what its comparison proves:
      *>   N1/N2 national and national-edited = SPACES -> SP
      *>   D3 display, P4 packed, F5 COMP-2 = 0        -> ZERO
      *>   G6 group: national leaf SPACES, numeric 0   -> OK
      *>   X7 alphanumeric = SPACES                    -> SP
      *>   B8 boolean PIC 1(3) displays its zeros      -> [000]
      *> Before PB2671 an omitted AREA national formal read its
      *> space-filled cell as byte pairs (NOT SPACES), an omitted
      *> AREA numeric read spaces, and an omitted carrier read the
      *> empty string.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2671OM.
       PROCEDURE DIVISION.
           DISPLAY "AREA FORMALS"
           CALL "PB2671OA" USING OMITTED OMITTED OMITTED OMITTED
               OMITTED OMITTED OMITTED OMITTED
           DISPLAY "CARRIER FORMALS"
           CALL "PB2671OB" USING OMITTED OMITTED OMITTED OMITTED
               OMITTED OMITTED OMITTED OMITTED
           STOP RUN.
       END PROGRAM PB2671OM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2671OA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       LINKAGE SECTION.
       01 L1 PIC N(2).
       01 L2 PIC NNBN.
       01 L3 PIC 9(3).
       01 L4 PIC S9(5) COMP-3.
       01 L5 COMP-2.
       01 L6.
          05 L6N PIC N(2).
          05 L6D PIC 9(2).
       01 L7 PIC X(3).
       01 L8 PIC 1(3).
       PROCEDURE DIVISION USING OPTIONAL L1 OPTIONAL L2 OPTIONAL L3
               OPTIONAL L4 OPTIONAL L5 OPTIONAL L6 OPTIONAL L7
               OPTIONAL L8.
           SET P TO ADDRESS OF L1
           SET P TO ADDRESS OF L2
           SET P TO ADDRESS OF L3
           SET P TO ADDRESS OF L4
           SET P TO ADDRESS OF L5
           SET P TO ADDRESS OF L7
           SET P TO ADDRESS OF L8
           PERFORM SHOW
           GOBACK.
       SHOW.
           IF L1 = SPACES DISPLAY "N1 SP" ELSE DISPLAY "N1 BAD" END-IF
           IF L2 = SPACES DISPLAY "N2 SP" ELSE DISPLAY "N2 BAD" END-IF
           IF L3 = 0 DISPLAY "D3 ZERO" ELSE DISPLAY "D3 BAD" END-IF
           IF L4 = 0 DISPLAY "P4 ZERO" ELSE DISPLAY "P4 BAD" END-IF
           IF L5 = 0 DISPLAY "F5 ZERO" ELSE DISPLAY "F5 BAD" END-IF
           IF L6N = SPACES AND L6D = 0
               DISPLAY "G6 OK"
           ELSE
               DISPLAY "G6 BAD"
           END-IF
           IF L7 = SPACES DISPLAY "X7 SP" ELSE DISPLAY "X7 BAD" END-IF
           DISPLAY "B8 [" L8 "]".
       END PROGRAM PB2671OA.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2671OB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC N(2).
       01 L2 PIC NNBN.
       01 L3 PIC 9(3).
       01 L4 PIC S9(5) COMP-3.
       01 L5 COMP-2.
       01 L6.
          05 L6N PIC N(2).
          05 L6D PIC 9(2).
       01 L7 PIC X(3).
       01 L8 PIC 1(3).
       PROCEDURE DIVISION USING OPTIONAL L1 OPTIONAL L2 OPTIONAL L3
               OPTIONAL L4 OPTIONAL L5 OPTIONAL L6 OPTIONAL L7
               OPTIONAL L8.
           PERFORM SHOW
           GOBACK.
       SHOW.
           IF L1 = SPACES DISPLAY "N1 SP" ELSE DISPLAY "N1 BAD" END-IF
           IF L2 = SPACES DISPLAY "N2 SP" ELSE DISPLAY "N2 BAD" END-IF
           IF L3 = 0 DISPLAY "D3 ZERO" ELSE DISPLAY "D3 BAD" END-IF
           IF L4 = 0 DISPLAY "P4 ZERO" ELSE DISPLAY "P4 BAD" END-IF
           IF L5 = 0 DISPLAY "F5 ZERO" ELSE DISPLAY "F5 BAD" END-IF
           IF L6N = SPACES AND L6D = 0
               DISPLAY "G6 OK"
           ELSE
               DISPLAY "G6 BAD"
           END-IF
           IF L7 = SPACES DISPLAY "X7 SP" ELSE DISPLAY "X7 BAD" END-IF
           DISPLAY "B8 [" L8 "]".
       END PROGRAM PB2671OB.

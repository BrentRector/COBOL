      *> kb/Work PB2078 - an object property that RECEIVES is accessed when its statement reaches it, receiver by receiver.
      *>
      *> THE RULE. ISO/IEC 1989:2023 14.7.7 4) b): the intermediate result "is stored in or combined with and then stored
      *> in each single resulting data item in the left-to-right order", and "Item identification for the receiving data
      *> items is done as each data item is accessed". 14.9.25.4 GR1 says the same of MOVE: "Item identification for
      *> identifier-2 is performed immediately before the data is moved to the respective data item". An object property
      *> stands in for its data item (8.4.3.9.4 GR1-GR3): the GET that fetches it belongs just before ITS access, the SET
      *> that assigns it just after ITS store (the operands of the initial evaluation, a sending property included, are
      *> identified "at the start of the execution of the statement", 14.7.7 4) a)). "If the size error condition is
      *> raised when attempting to store in a resulting data item, only that data item remains unchanged" (14.7.7 4) b)),
      *> so no SET assigns it.
      *>
      *> The class counts its accessor calls: SHOW prints, after a space, the object's value (B), its GET count (G) and
      *> its SET count (S); the S lines print one SHOW line per object, the D lines continue the line their label began.
      *> EXPECTED OUTPUT (each value derived from those rules):
      *>   S1 I=2 / B=00010 G=00 S=00 / B=00021 G=01 S=01
      *>        ADD 1 TO I, BAL OF AR(I) with I = 1: I is stored first (14.9.2.4: the operands before TO are evaluated, then
      *>        the sum is stored in each receiver in turn), so BAL OF AR(I) is identified as AR(2): 20 + 1 = 21 by one GET
      *>        and one SET. AR(1) is never touched.
      *>   S2 I=2 / B=00002 G=00 S=01 / B=00021 G=01 S=01
      *>        MOVE 2 TO BAL OF AR(I), I with I = 1: the first receiver is identified before I is stored (14.9.25.4 GR1),
      *>        so AR(1) receives 2 by one SET (a receiving-only property is not fetched); AR(2) keeps S1's state.
      *>   S3 I=2 / B=99999 G=01 S=00 / B=00021 G=01 S=01
      *>        AR(1) re-seeded to 99999: ADD 1 TO BAL OF AR(I) ON SIZE ERROR MOVE 2 TO I. The GET fetched 99999, the sum
      *>        100000 does not fit PIC 9(5): a size error, AR(1) is unchanged and not assigned (S stays 0), and the
      *>        imperative sets I to 2 AFTER the receiver was identified. AR(2) is untouched.
      *>   D1 B=00102 G=02 S=02   ADD 1 TO BAL OF D, BAL OF D: GET 100, SET 101, then the second receiver's GET 101, SET 102.
      *>   D2 B=00100 G=04 S=04   SUBTRACT 1 FROM BAL OF D, BAL OF D: 102 - 1 - 1.
      *>   D3 B=00400 G=06 S=06   MULTIPLY 2 BY BAL OF D, BAL OF D: 100 * 2 * 2.
      *>   D4 B=00100 G=08 S=08   DIVIDE 2 INTO BAL OF D, BAL OF D: 400 / 2 / 2.
      *>   D5 B=00005 G=08 S=10   MOVE 5 TO BAL OF D, BAL OF D: two SETs, no GET.
      *>   D6 B=00006 G=09 S=12   COMPUTE BAL OF D, BAL OF D = BAL OF D + 1: the sending BAL OF D is one GET and the
      *>        initial evaluation (5 + 1) is stored in each receiver (14.9.8: GIVING-style resultants), two SETs, no GET.
      *>   D7 B=00007 G=09 S=14   ADD 3 4 GIVING BAL OF D, BAL OF D: 7 stored twice.
      *>   D8 B=00001 G=09 S=15   DIVIDE 7 BY 2 GIVING Q REMAINDER BAL OF D: Q = 3, the remainder 7 - 3 * 2 = 1 is SET.
      *>   F1 B=00040 G=09 S=16   MOVE 40 TO BAL OF FUNCTION PB2078ID (D): the receiver's object is a function-identifier's
      *>        result (8.4.3.1.3 SR1: identifier-3 is any identifier); the function is activated when the MOVE reaches the
      *>        receiver, once, and the one SET assigns the object it returned (D's own).
      *>   F2 B=00042 G=10 S=17   ADD 2 TO BAL OF FUNCTION PB2078ID (D): one activation, one GET (40), one SET (42).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB2078ID.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB2078.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-IN USAGE OBJECT REFERENCE CPB2078.
       01 L-OBJ USAGE OBJECT REFERENCE CPB2078.
       PROCEDURE DIVISION USING L-IN RETURNING L-OBJ.
           SET L-OBJ TO L-IN
           GOBACK.
       END FUNCTION PB2078ID.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2078RP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB2078
           FUNCTION PB2078ID
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT TYPEDEF STRONG.
          05 AR USAGE OBJECT REFERENCE CPB2078 OCCURS 2.
       01 R TYPE RT.
       01 D USAGE OBJECT REFERENCE CPB2078.
       01 I PIC 9.
       01 Q PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB2078 "NEW" RETURNING AR(1)
           INVOKE CPB2078 "NEW" RETURNING AR(2)
           INVOKE CPB2078 "NEW" RETURNING D
           INVOKE AR(1) "SEED" USING 10
           INVOKE AR(2) "SEED" USING 20
           INVOKE D "SEED" USING 100
           MOVE 1 TO I
           ADD 1 TO I, BAL OF AR(I)
           DISPLAY "S1 I=" I
           INVOKE AR(1) "SHOW"
           INVOKE AR(2) "SHOW"
           MOVE 1 TO I
           MOVE 2 TO BAL OF AR(I), I
           DISPLAY "S2 I=" I
           INVOKE AR(1) "SHOW"
           INVOKE AR(2) "SHOW"
           INVOKE AR(1) "SEED" USING 99999
           MOVE 1 TO I
           ADD 1 TO BAL OF AR(I) ON SIZE ERROR MOVE 2 TO I
           END-ADD
           DISPLAY "S3 I=" I
           INVOKE AR(1) "SHOW"
           INVOKE AR(2) "SHOW"
           ADD 1 TO BAL OF D, BAL OF D
           DISPLAY "D1" WITH NO ADVANCING
           INVOKE D "SHOW"
           SUBTRACT 1 FROM BAL OF D, BAL OF D
           DISPLAY "D2" WITH NO ADVANCING
           INVOKE D "SHOW"
           MULTIPLY 2 BY BAL OF D, BAL OF D
           DISPLAY "D3" WITH NO ADVANCING
           INVOKE D "SHOW"
           DIVIDE 2 INTO BAL OF D, BAL OF D
           DISPLAY "D4" WITH NO ADVANCING
           INVOKE D "SHOW"
           MOVE 5 TO BAL OF D, BAL OF D
           DISPLAY "D5" WITH NO ADVANCING
           INVOKE D "SHOW"
           COMPUTE BAL OF D, BAL OF D = BAL OF D + 1
           DISPLAY "D6" WITH NO ADVANCING
           INVOKE D "SHOW"
           ADD 3 4 GIVING BAL OF D, BAL OF D
           DISPLAY "D7" WITH NO ADVANCING
           INVOKE D "SHOW"
           DIVIDE 7 BY 2 GIVING Q REMAINDER BAL OF D
           DISPLAY "D8" WITH NO ADVANCING
           INVOKE D "SHOW"
           MOVE 40 TO BAL OF FUNCTION PB2078ID (D)
           DISPLAY "F1" WITH NO ADVANCING
           INVOKE D "SHOW"
           ADD 2 TO BAL OF FUNCTION PB2078ID (D)
           DISPLAY "F2" WITH NO ADVANCING
           INVOKE D "SHOW"
           STOP RUN.
       END PROGRAM PB2078RP.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB2078 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-BAL PIC 9(5) VALUE 0.
       01 W-GETS PIC 99 VALUE 0.
       01 W-SETS PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. GET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(5).
       PROCEDURE DIVISION RETURNING LK-R.
           ADD 1 TO W-GETS
           MOVE W-BAL TO LK-R.
       END METHOD.
       IDENTIFICATION DIVISION.
       METHOD-ID. SET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(5).
       PROCEDURE DIVISION USING LK-V.
           ADD 1 TO W-SETS
           MOVE LK-V TO W-BAL.
       END METHOD.
       IDENTIFICATION DIVISION.
       METHOD-ID. SEED.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(5).
       PROCEDURE DIVISION USING LK-V.
           MOVE LK-V TO W-BAL
           MOVE 0 TO W-GETS
           MOVE 0 TO W-SETS.
       END METHOD SEED.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY " B=" W-BAL " G=" W-GETS " S=" W-SETS.
       END METHOD SHOW.
       END OBJECT.
       END CLASS CPB2078.

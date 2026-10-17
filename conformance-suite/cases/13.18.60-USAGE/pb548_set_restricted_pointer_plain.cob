*>--- conformance-case
*> id: pb548_set_restricted_pointer_plain
*> rules: [GR-13.18.60.4-23, SR-14.9.39.3-19]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB548 - the CONFORMING face of the third SET Format 7 arm: a plain pointer-item sender
      *> (`SET P2 TO P1`, setToValueStatement -> SetBinder.BindSetPointer) between two data-pointers
      *> restricted to the SAME type, and a TO NULL sender, both of which ISO 14.9.39.3 SR19 admits:
      *> "If identifier-5 references a restricted data-pointer, identifier-6 shall be the predefined
      *> address NULL or shall reference a data-pointer restricted to the same type."
      *> (python scripts/spec/cite.py --check 14.9.39.3 "If identifier-5 references a restricted
      *> data-pointer, identifier-6 shall be the predefined address NULL or shall reference a data-pointer
      *> restricted to the same type" -> OK 14.9.39.3 19))
      *> EXPECTED, from the rules:
      *>  . 14.9.39.4 GR12 - "the address identified by identifier-6 is stored in each data item
      *>    referenced by identifier-5" - so after SET P2 P3 TO P1 both hold ADDRESS OF V1, and the based
      *>    item L rebased through P3 reads V1's content: L=[ABCD].
      *>  . SET P1 TO NULL stores the predefined address NULL (GR12 again), so P1 = NULL is true: P1-NULL.
      *>  . P2 is untouched by the SET of P1, so L rebased through P2 still reads V1: L2=[ABCD].
      *> The unrestricted-into-restricted and restricted-into-unrestricted spellings of the same arm are
      *> the negatives pb548-set-unrestricted-into-restricted / pb548-set-restricted-into-unrestricted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB548POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT IS TYPEDEF STRONG.
          05 F1 PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO TT.
       01 V1 TYPE TT.
       01 P1 TYPE PT.
       01 P2 TYPE PT.
       01 P3 TYPE PT.
       01 L TYPE TT BASED.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCD" TO F1 OF V1
           SET P1 TO ADDRESS OF V1
           SET P2 P3 TO P1
           SET ADDRESS OF L TO P3
           DISPLAY "L=[" F1 OF L "]"
           SET P1 TO NULL
           IF P1 = NULL
               DISPLAY "P1-NULL"
           ELSE
               DISPLAY "P1-NOT-NULL"
           END-IF
           SET ADDRESS OF L TO P2
           DISPLAY "L2=[" F1 OF L "]"
           STOP RUN.

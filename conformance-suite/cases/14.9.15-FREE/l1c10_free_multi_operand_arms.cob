      *>--- conformance-case
      *> id: l1c10_free_multi_operand_arms
      *> rules: [GR-14.9.15.4-1, GR-14.9.15.4-2, FMT-14.9.15.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.15 FREE — format, GR1 a) b) c) and GR2 order/resume
      *>
      *> Rows pinned: FMT-14.9.15.2, GR-14.9.15.4-1, GR-14.9.15.4-2.
      *>  Format "FREE { data-name-1 } ..." (one or more operands).
      *>  GR1 "a) If the data-pointer referenced by data-name-1
      *>    identifies the start of storage that is currently
      *>    allocated by an ALLOCATE statement, that storage is
      *>    released and the data-pointer ... is set to NULL ...
      *>    b) otherwise, if the data-pointer ... contains the
      *>    predefined address NULL, no action is taken for that
      *>    operand; c) otherwise, the EC-STORAGE-NOT-ALLOC exception
      *>    condition is set to exist."
      *>  GR2 "... the same as if a separate FREE statement had been
      *>    written for each data-name-1 in the same order as
      *>    specified in the FREE statement. If an implicit FREE
      *>    statement results in an exception condition being raised
      *>    and the exception condition is nonfatal, after any
      *>    applicable exception processing statements are executed,
      *>    processing resumes at the next implicit FREE statement,
      *>    if any. ... If there is no next implicit FREE statement,
      *>    processing resumes at the next executable statement".
      *>  EC-STORAGE-NOT-ALLOC is NF (nonfatal) in the exception
      *>  table; with checking ON, 14.6.13.1.4 3) runs the USE
      *>  declarative and "If execution of the declarative completes
      *>  normally, execution continues as specified in the rules
      *>  for normal execution" (here: GR2's resumption).
      *> cite.py --check:
      *>  OK  §14.9.15.2   (General format)
      *>  OK  §14.9.15.4 1) a)  (General rules)
      *>  OK  §14.9.15.4 1) b)  (General rules)
      *>  OK  §14.9.15.4 1) c)  (General rules)
      *>  OK  §14.9.15.4 2)  (General rules)
      *>  OK  §14.6.13.1.4 3)  (Nonfatal exception conditions)
      *>
      *> Derivation of each expected line (EC lines come from the
      *> declarative; K counts them):
      *>  S1: P and Q address the same ALLOCATEd storage; N is NULL.
      *>    FREE P N Q in source order: P is the start of allocated
      *>    storage -> a) released, P NULL; N NULL -> b) nothing (no
      *>    EC); Q now addresses storage no longer currently allocated
      *>    and is not NULL -> c) EC. Reverse order would free via Q
      *>    and raise on P, so the P/Q result pins the order.
      *>    EC1=EC-STORAGE-NOT-ALLOC
      *>    S1 P=NULL N=NULL Q=SAME
      *>  S2: R addresses WORKING-STORAGE W (never ALLOCATEd), P a
      *>    fresh allocation. FREE R P: R -> c) EC, R unchanged; the
      *>    EC is nonfatal so processing resumes at the next implicit
      *>    FREE: P -> a) NULL.
      *>    EC2=EC-STORAGE-NOT-ALLOC
      *>    S2 R=SAME P=NULL
      *>  S3: Q = fresh allocation address UP BY 1 (not the START of
      *>    the storage). FREE Q P: Q -> c) EC, Q unchanged; resume:
      *>    P -> a) NULL; then the next statement.
      *>    EC3=EC-STORAGE-NOT-ALLOC
      *>    S3 Q=SAME P=NULL
      *>  END K=3   exactly three conditions: b) raised none.
      *> (Each EC line ends in the 31-char EXCEPTION-STATUS value;
      *> trailing spaces are not compared. T1-T3 are PIC X(8), so a
      *> MOVE of "NULL"/"SAME" is space-filled on the right to 8
      *> characters: the .out carries those inner spaces.)
       >>TURN EC-STORAGE-NOT-ALLOC CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C10E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P  USAGE POINTER.
       01 Q  USAGE POINTER.
       01 N  USAGE POINTER.
       01 R  USAGE POINTER.
       01 QS USAGE POINTER.
       01 W  PIC X(4) VALUE "WSWS".
       01 K  PIC 9 VALUE 0.
       01 T1 PIC X(8).
       01 T2 PIC X(8).
       01 T3 PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       HNA SECTION.
           USE AFTER EXCEPTION CONDITION EC-STORAGE-NOT-ALLOC.
       HNA-P.
           ADD 1 TO K.
           DISPLAY "EC" K "=" FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> S1: arms a) b) c) in one FREE, source order.
           ALLOCATE 4 CHARACTERS RETURNING P.
           SET Q TO P.
           SET QS TO Q.
           SET N TO NULL.
           FREE P N Q.
           MOVE "NOTNULL" TO T1 T2.
           MOVE "CHANGED" TO T3.
           IF P = NULL MOVE "NULL" TO T1.
           IF N = NULL MOVE "NULL" TO T2.
           IF Q = QS MOVE "SAME" TO T3.
           DISPLAY "S1 P=" T1 " N=" T2 " Q=" T3.
      *> S2: storage not obtained by ALLOCATE, then resume.
           SET R TO ADDRESS OF W.
           SET QS TO R.
           ALLOCATE 4 CHARACTERS RETURNING P.
           FREE R P.
           MOVE "CHANGED" TO T1.
           MOVE "NOTNULL" TO T2.
           IF R = QS MOVE "SAME" TO T1.
           IF P = NULL MOVE "NULL" TO T2.
           DISPLAY "S2 R=" T1 " P=" T2.
      *> S3: not the start of allocated storage, then resume.
           ALLOCATE 4 CHARACTERS RETURNING P.
           SET Q TO P.
           SET Q UP BY 1.
           SET QS TO Q.
           FREE Q P.
           MOVE "CHANGED" TO T1.
           MOVE "NOTNULL" TO T2.
           IF Q = QS MOVE "SAME" TO T1.
           IF P = NULL MOVE "NULL" TO T2.
           DISPLAY "S3 Q=" T1 " P=" T2.
           DISPLAY "END K=" K.
           STOP RUN.

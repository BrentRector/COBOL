*>--- conformance-case
*> id: pb133_gr3a_once_only
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 wave B - ISO 14.9.4.4 GR3a: "item identification is done ... at the beginning of
      *> the execution of the CALL statement", and 14.2.3 GR8 fixes each BY REFERENCE argument's storage
      *> area at the same point. The callee re-aims the caller's subscript I (2) THROUGH the second
      *> reference argument, then stores into the first formal - the store shall land in E(1), the
      *> element identified at the CALL's start, never E(2). Derived: E1=7777 E2=0000 I=2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR3AP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          02 E PIC 9(4) OCCURS 3.
       01 I PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0 TO E(1) E(2) E(3)
           CALL "GR3AS" AS NESTED USING E(I) I
           DISPLAY "E1=" E(1) " E2=" E(2) " I=" I
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR3AS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-E PIC 9(4).
       01 L-I PIC 9.
       PROCEDURE DIVISION USING L-E L-I.
       P.
           MOVE 2 TO L-I
           MOVE 7777 TO L-E
           GOBACK.
       END PROGRAM GR3AS.
       END PROGRAM GR3AP.

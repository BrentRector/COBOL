*>--- conformance-case
*> id: pb613_gr8_one_reduction
*> rules: [SR-14.9.4.3-17, GR-14.9.4.4-8, GR-14.9.4.4-9, GR-14.9.23.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB613 / PB3101 - an activation argument's parse node is reduced ONCE, by 14.9.4.4 GR8 (CALL)
      *> and its INVOKE twin 14.9.23.4 GR4, before its shape is dispatched. In each statement below the LAST
      *> argument is a boolean expression, so the parser's look-ahead puts the earlier keyword-less argument in
      *> a boolean node too; GR8 makes it the identifier or literal it consists of all the same:
      *>  G1 CALL ... USING N B1 B-AND B2: N is identifier-2 and meets Syntax rule 3, and the formal is
      *>     BY REFERENCE, so GR9 a) 1. assumes BY REFERENCE; the callee's MOVE 42 reaches N, "N=0042".
      *>     (The keyword-less boolean arm used to pass N BY CONTENT: "N=0001".)
      *>  G2 CALL ... USING 5 B1 B-AND B2 into a BY VALUE formal: 5 is literal-2, and GR9 b) assumes BY
      *>     VALUE, "X=0005".
      *>  G3 CALL ... USING "AB" B1 B-AND B2 into a PIC XX formal: literal-2 BY CONTENT, "Q=AB".
      *>  G4 the G1 shape through INVOKE (14.9.23.4 GR6 a) 1.): "N=0042".
      *>  G5 INVOKE ... USING 7 "AB" N3 with formals PIC 9, PIC XX, PIC 9(4) and N3 = 5: "7AB0005".
      *>  Y is B1 B-AND B2 = 1100 B-AND 1010 = "1000" (8.8.2).
      *> cite.py --check 14.9.4.4 "An argument that consists merely of a single identifier or literal is
      *>   regarded as an identifier or literal rather than an arithmetic or boolean expression"
      *>   -> OK 14.9.4.4 8)
      *> cite.py --check 14.9.23.4 (the same sentence) -> OK 14.9.23.4 4)
      *> cite.py --check 14.9.4.4 "if the argument meets the requirements of Syntax rule 3, BY REFERENCE is
      *>   assumed" -> OK 14.9.4.4 9) a) 1.
      *> cite.py --check 14.9.4.4 "When the BY VALUE phrase is specified or implied for the corresponding
      *>   formal parameter, BY VALUE is assumed" -> OK 14.9.4.4 9) b)
      *> cite.py --check 14.9.23.4 "if the argument meets the requirements of Syntax rules 9 and 10, BY
      *>   REFERENCE is assumed" -> OK 14.9.23.4 6)
       IDENTIFICATION DIVISION.
       CLASS-ID. K613G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K613G.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. MR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9(4).
       01 Y PIC 1(4).
       PROCEDURE DIVISION USING X Y.
           DISPLAY "G4 Y=" Y
           MOVE 42 TO X
           GOBACK.
       END METHOD MR.
       METHOD-ID. MT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9.
       01 Q PIC XX.
       01 R PIC 9(4).
       PROCEDURE DIVISION USING P Q R.
           DISPLAY "G5 " P Q R
           GOBACK.
       END METHOD MT.
       END FACTORY.
       END CLASS K613G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P613G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K613G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 1.
       01 N2 PIC 9(4) VALUE 1.
       01 N3 PIC 9(4) VALUE 5.
       01 B1 PIC 1(4) VALUE B"1100".
       01 B2 PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
           CALL "S613R" AS NESTED USING N B1 B-AND B2
           DISPLAY "G1 N=" N
           CALL "S613V" AS NESTED USING 5 B1 B-AND B2
           CALL "S613L" AS NESTED USING "AB" B1 B-AND B2
           INVOKE K613G "MR" USING N2 B1 B-AND B2
           DISPLAY "G4 N=" N2
           INVOKE K613G "MT" USING 7 "AB" N3
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S613R.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9(4).
       01 Y PIC 1(4).
       PROCEDURE DIVISION USING X Y.
           DISPLAY "G1 Y=" Y
           MOVE 42 TO X
           GOBACK.
       END PROGRAM S613R.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S613V.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9(4) BINARY.
       01 Y PIC 1(4).
       PROCEDURE DIVISION USING BY VALUE X BY REFERENCE Y.
           DISPLAY "G2 X=" X " Y=" Y
           GOBACK.
       END PROGRAM S613V.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S613L.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Q PIC XX.
       01 Y PIC 1(4).
       PROCEDURE DIVISION USING Q Y.
           DISPLAY "G3 Q=" Q " Y=" Y
           GOBACK.
       END PROGRAM S613L.
       END PROGRAM P613G.

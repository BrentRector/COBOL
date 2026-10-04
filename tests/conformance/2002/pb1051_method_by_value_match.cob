      *> kb/Work PB1051 -- BY VALUE method formals in METHOD RESOLUTION (ISO 9.3.6 match rule 5).
      *> 9.3.6 match rule 5 (cite.py --check 9.3.6 "For each parameter that is passed by value there shall be a
      *> corresponding parameter in the invoked method" -> OK 9.3.6 5)): a by-value parameter needs a BY VALUE formal and
      *> "may be a receiving data item in a SET/MOVE statement" with the parameter as the sender - 14.8.2.3.3 rule 2 a)
      *> makes a numeric formal's rule the COMPUTE statement's, so a PIC 9(3) DISPLAY argument conforms to a
      *> PIC S9(4) COMP-5 formal (a NON-EXACT match, bound by step 3/4). The method is INHERITED: it is declared in CBM
      *> and invoked on a DBM object (resolution step 2/4: each inherited class upward is inspected).
      *> Resolution step 5 (cite.py --check 9.3.6 "steps 3 and 4 are repeated, ignoring the requirements specified in
      *> 4c, 4d, 5c and 5d below" -> OK 9.3.6 5)): a literal whose value would TRUNCATE still matches, so
      *> USING BY VALUE 12345 into PIC 9(3) delivers 345 (high-order digits truncated, as MOVE and COMPUTE give).
      *> 13.7.4 GR4 (cite.py --check 13.7.4 "access to formal parameters and the returning item is always
      *> guaranteed" -> OK 13.7.4 4)): the RETURNING item of a method with a BY VALUE formal is delivered.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1051M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBM
           CLASS DBM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE DBM.
       01 W3 PIC 9(3) VALUE 321.
       01 SUM-OUT PIC S9(6) COMP-5.
       01 E PIC -(6)9.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE DBM "NEW" RETURNING O.
           INVOKE O "ADD2" USING BY VALUE W3 10 RETURNING SUM-OUT.
           MOVE SUM-OUT TO E.
           DISPLAY "SUM=" E.
           INVOKE O "TR" USING BY VALUE 12345.
           STOP RUN.
       END PROGRAM PB1051M.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBM INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. ADD2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC S9(4) COMP-5.
       01 B PIC S9(4) COMP-5.
       01 R PIC S9(6) COMP-5.
       PROCEDURE DIVISION USING BY VALUE A B RETURNING R.
           COMPUTE R = A + B.
       END METHOD ADD2.
       METHOD-ID. TR.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 E PIC 9(3).
       LINKAGE SECTION.
       01 T PIC 9(3).
       PROCEDURE DIVISION USING BY VALUE T.
           MOVE T TO E.
           DISPLAY "T=" E.
       END METHOD TR.
       END OBJECT.
       END CLASS CBM.

       IDENTIFICATION DIVISION.
       CLASS-ID. DBM INHERITS FROM CBM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBM.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS DBM.

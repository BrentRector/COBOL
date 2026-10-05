      *> ISO §14.9.24.4 GR14 -> §12.4.5.3 GR3 b) — a MERGE associates
      *> each USING/GIVING file with the physical file its ASSIGN USING
      *> data item names AT THE TIME THE MERGE EXECUTES.
      *>
      *> GR14: "Additional rules affecting the execution of the MERGE
      *> statement are given in 12.4.5, File control entry, General
      *> rules 3 and 4."
      *>   cite.py: OK  §14.9.24.4 14)  (General rules)
      *> 12.4.5.3 GR3: "The association occurs at the time of execution
      *> of an OPEN, SORT, or MERGE statement that referenced
      *> file-name-1"; b) "the file connector referenced by file-name-1
      *> is associated with a physical file identified by the content
      *> of the data item referenced by data-name-1 in the runtime
      *> element that executes the OPEN, SORT, or MERGE statement."
      *>   cite.py: OK  §12.4.5.3 3)  (General rules)
      *>   cite.py: OK  §12.4.5.3 3) b)  (General rules)
      *> Closing sentence: "If the association cannot be made because
      *> the content of the data item referenced by data-name-1 is not
      *> consistent ..., the OPEN, SORT, or MERGE statement is
      *> unsuccessful."  GR4 leaves the content rules to the
      *> implementor: docs/CONFORMANCE.md DOC-A.1-73 (content that is
      *> empty after space removal names no physical file -> the
      *> association cannot be made -> '31', §9.1.13.6 item 2).
      *> §9.1.13.1: "control is transferred to the end of the statement
      *> that produced the fatal exception condition unless the rules
      *> for that statement define other behavior" (COBOL.NET continues
      *> the run unit; CONFORMANCE.md, SORT/MERGE implicit transfers
      *> (a)).
      *>   cite.py: OK  §9.1.13.1   (General)
      *>
      *> Setup (via CK, itself ASSIGN USING NC): l1c18ba.dat = AAA,
      *> l1c18bb.dat = BBB, l1c18bc.dat (I2's fixed file) = CCC.
      *> Derivation:
      *>  M1: N1 = "l1c18ba.dat", NG = "l1c18bo1.dat" at MERGE time ->
      *>      I1 reads AAA, I2 reads CCC, ascending -> GIVING file
      *>      l1c18bo1.dat holds AAA, CCC.  The implicit CLOSE of I1
      *>      (GR7 c) leaves S1 = "00".  Lines "M1 S1=00", "O AAA",
      *>      "O CCC".
      *>  M2: the SAME connectors, N1 = "l1c18bb.dat", NG =
      *>      "l1c18bo2.dat": association is redone at this MERGE, so
      *>      l1c18bo2.dat holds BBB, CCC (an association fixed at the
      *>      first MERGE would give AAA again / write l1c18bo1.dat).
      *>      Lines "M2 S1=00", "O BBB", "O CCC".  Reading l1c18bo1.dat
      *>      again afterwards still gives AAA, CCC: "P AAA", "P CCC".
      *>  M3: N1 = SPACES: the association cannot be made -> the as-if
      *>      OPEN of I1 is unsuccessful with '31' (DOC-A.1-73); the
      *>      USE procedure on I1 runs ("USE S1=31"); '31' is fatal so
      *>      control goes to the end of the MERGE: "M3 S1=31".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C18B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT I1 ASSIGN USING N1
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS S1.
           SELECT I2 ASSIGN TO "l1c18bc.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT GO1 ASSIGN USING NG
               ORGANIZATION IS SEQUENTIAL.
           SELECT CK ASSIGN USING NC
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SC.
           SELECT SM ASSIGN TO "l1c18bs.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  I1.
       01  I1-REC        PIC X(3).
       FD  I2.
       01  I2-REC        PIC X(3).
       FD  GO1.
       01  GO1-REC       PIC X(3).
       FD  CK.
       01  CK-REC        PIC X(3).
       SD  SM.
       01  SM-REC        PIC X(3).
       WORKING-STORAGE SECTION.
       01  N1            PIC X(20).
       01  NG            PIC X(20).
       01  NC            PIC X(20).
       01  S1            PIC XX.
       01  SC            PIC XX.
       01  TAG           PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       I1-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON I1.
       I1-ERR-P.
           DISPLAY "USE S1=" S1.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           MOVE "l1c18ba.dat" TO NC.
           OPEN OUTPUT CK.
           MOVE "AAA" TO CK-REC.
           WRITE CK-REC.
           CLOSE CK.
           MOVE "l1c18bb.dat" TO NC.
           OPEN OUTPUT CK.
           MOVE "BBB" TO CK-REC.
           WRITE CK-REC.
           CLOSE CK.
           OPEN OUTPUT I2.
           MOVE "CCC" TO I2-REC.
           WRITE I2-REC.
           CLOSE I2.
           MOVE "l1c18ba.dat" TO N1.
           MOVE "l1c18bo1.dat" TO NG.
           MERGE SM ON ASCENDING KEY SM-REC
               USING I1 I2 GIVING GO1.
           DISPLAY "M1 S1=" S1.
           MOVE "l1c18bo1.dat" TO NC.
           MOVE "O" TO TAG.
           PERFORM SHOW-FILE.
           MOVE "l1c18bb.dat" TO N1.
           MOVE "l1c18bo2.dat" TO NG.
           MERGE SM ON ASCENDING KEY SM-REC
               USING I1 I2 GIVING GO1.
           DISPLAY "M2 S1=" S1.
           MOVE "l1c18bo2.dat" TO NC.
           PERFORM SHOW-FILE.
           MOVE "l1c18bo1.dat" TO NC.
           MOVE "P" TO TAG.
           PERFORM SHOW-FILE.
           MOVE SPACES TO N1.
           MOVE "l1c18bo3.dat" TO NG.
           MERGE SM ON ASCENDING KEY SM-REC
               USING I1 I2 GIVING GO1.
           DISPLAY "M3 S1=" S1.
           STOP RUN.
       SHOW-FILE.
           OPEN INPUT CK.
           PERFORM UNTIL SC NOT = "00"
               READ CK
                   AT END CONTINUE
                   NOT AT END DISPLAY TAG " " CK-REC
               END-READ
           END-PERFORM.
           CLOSE CK.

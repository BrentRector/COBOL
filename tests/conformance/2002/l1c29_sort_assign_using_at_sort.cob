      *> ISO §14.9.40.4 GR6 — SORT associates USING/GIVING files at the SORT (§12.4.5.3 GR3 b)
      *> Rule (§14.9.40.4 GR6): "Additional rules affecting the execution of
      *> the SORT statement are given in 12.4.5, File control entry, General
      *> rules 3 and 4."
      *> cite.py --check 14.9.40.4 "Additional rules affecting the execution
      *>   of the SORT statement are given in 12.4.5" -> OK  §14.9.40.4 6)
      *> §12.4.5.3 GR3: "The association occurs at the time of execution of
      *> an OPEN, SORT, or MERGE statement that referenced file-name-1"
      *> cite.py --check 12.4.5.3 "The association occurs at the time of
      *>   execution of an OPEN, SORT, or MERGE statement that referenced
      *>   file-name-1" -> OK  §12.4.5.3 3)
      *> §12.4.5.3 GR3 b): "... associated with a physical file identified
      *> by the content of the data item referenced by data-name-1 in the
      *> runtime element that executes the OPEN, SORT, or MERGE statement"
      *> cite.py --check 12.4.5.3 "is associated with a physical file
      *>   identified by the content of the data item referenced by
      *>   data-name-1 in the runtime element that executes the OPEN, SORT,
      *>   or MERGE statement" -> OK  §12.4.5.3 3) b)
      *> GR4 (content meaning) is the documented choice DOC-A.1-73 in
      *> docs/CONFORMANCE.md: the content with spaces removed is the target,
      *> and a target containing "." is used verbatim.
      *> Edition: ASSIGN USING (dynamic file assignment, §9.1.21) is a 2002
      *> introduction; ISO_COBOL.md itself does not date it, and the edge
      *> is derived from GnuCOBOL's per-standard dialect files (constructs
      *> row assign-using-2002, VCR row 7.27; kb/Work PB746).
      *> Setup: both connectors are last OPENed on a DIFFERENT file than
      *> the one named at the SORT: FI last opened l1c29b-1.dat (Z1,Y1),
      *> FO last opened l1c29b-q.dat (QQ). Before the SORT the names are
      *> set to l1c29b-2.dat (B2,A2) and l1c29b-o.dat (holding STALE).
      *> Derivation:
      *>  GR3 b): the SORT reads l1c29b-2.dat and writes l1c29b-o.dat, so
      *>  l1c29b-o.dat holds B2,A2 sorted ascending on the first byte:
      *>  "OUT-O A2", "OUT-O B2" (STALE is replaced: the implicit OPEN
      *>  OUTPUT of GIVING recreates the file). l1c29b-q.dat is untouched:
      *>  "OUT-Q QQ". An implementation associating at the last OPEN would
      *>  print Y1/Z1 and/or a changed Q file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C29B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FI ASSIGN USING FI-NAME
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FI-ST.
           SELECT FO ASSIGN USING FO-NAME
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FO-ST.
           SELECT SF ASSIGN TO "L1C29B.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD  FI.
       01  FI-REC PIC X(2).
       FD  FO.
       01  FO-REC PIC X(5).
       SD  SF.
       01  SR.
           05 SK PIC X(1).
           05 SV PIC X(4).
       WORKING-STORAGE SECTION.
       01  FI-NAME PIC X(20) VALUE SPACES.
       01  FO-NAME PIC X(20) VALUE SPACES.
       01  FI-ST   PIC XX.
       01  FO-ST   PIC XX.
       01  EOF-FLAG PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "l1c29b-2.dat" TO FI-NAME.
           OPEN OUTPUT FI.
           MOVE "B2" TO FI-REC. WRITE FI-REC.
           MOVE "A2" TO FI-REC. WRITE FI-REC.
           CLOSE FI.
           MOVE "l1c29b-1.dat" TO FI-NAME.
           OPEN OUTPUT FI.
           MOVE "Z1" TO FI-REC. WRITE FI-REC.
           MOVE "Y1" TO FI-REC. WRITE FI-REC.
           CLOSE FI.
           MOVE "l1c29b-o.dat" TO FO-NAME.
           OPEN OUTPUT FO.
           MOVE "STALE" TO FO-REC. WRITE FO-REC.
           CLOSE FO.
           MOVE "l1c29b-q.dat" TO FO-NAME.
           OPEN OUTPUT FO.
           MOVE "QQ" TO FO-REC. WRITE FO-REC.
           CLOSE FO.
           MOVE "l1c29b-2.dat" TO FI-NAME.
           MOVE "l1c29b-o.dat" TO FO-NAME.
           SORT SF ON ASCENDING KEY SK
               USING FI
               GIVING FO.
           OPEN INPUT FO.
           MOVE "N" TO EOF-FLAG.
           PERFORM UNTIL EOF-FLAG = "Y"
               READ FO
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "OUT-O " FO-REC
               END-READ
           END-PERFORM.
           CLOSE FO.
           MOVE "l1c29b-q.dat" TO FO-NAME.
           OPEN INPUT FO.
           MOVE "N" TO EOF-FLAG.
           PERFORM UNTIL EOF-FLAG = "Y"
               READ FO
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "OUT-Q " FO-REC
               END-READ
           END-PERFORM.
           CLOSE FO.
           DISPLAY "END".
           STOP RUN.

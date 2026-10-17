*>--- conformance-case
*> id: l1c29-sort-file-not-sd
*> rules: [SR-14.9.40.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.40.3 SR4 — SORT file-name-1 described by an FD, not an SD
      *> Rule: "File-name-1 shall be described in a sort-merge file
      *> description entry in the data division."
      *> cite.py --check 14.9.40.3 "File-name-1 shall be described in a
      *>   sort-merge file description entry in the data division"
      *>   -> OK  §14.9.40.3 4)  (Syntax rules)
      *> WF is a sequential FD. Everything else is legal: the key WK is in
      *> WF's record (SR6 a), UF/GF are FDs (SR8), all records are 4
      *> character positions (SR5/SR11), and the program declares an SD
      *> that it simply does not name. Only SR4 is violated.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C29G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT UF ASSIGN TO "L1C29GI.DAT".
           SELECT GF ASSIGN TO "L1C29GO.DAT".
           SELECT WF ASSIGN TO "L1C29GW.DAT".
           SELECT SF ASSIGN TO "L1C29GS.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD  UF.
       01  UF-REC PIC X(4).
       FD  GF.
       01  GF-REC PIC X(4).
       FD  WF.
       01  WR.
           05 WK PIC X(1).
           05 WV PIC X(3).
       SD  SF.
       01  SR PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT WF ON ASCENDING KEY WK
               USING UF
               GIVING GF.
           STOP RUN.

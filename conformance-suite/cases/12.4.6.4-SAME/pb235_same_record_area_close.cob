      *>--- conformance-case
      *> id: pb235_same_record_area_close
      *> rules: [GR-12.4.6.4.4-2, GR-14.9.6.4-7, DOC-A.1-24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB235SRA.
      *> kb/Work PB235 - ISO 14.9.6.4 GR7, the availability of the record
      *> area after a CLOSE, over a SAME RECORD AREA group. Every value
      *> below is DERIVED from the rule, not observed:
      *>
      *> GR7: "If file-name-1 is specified in a SAME RECORD AREA clause,
      *> the record area is available to the runtime element if any of the
      *> file connectors referenced by the other file-names in that SAME
      *> RECORD AREA clause are open." 12.4.6.4.4 GR2 says the same thing
      *> from the clause's side and adds which record it holds: "A logical
      *> record in the shared memory area is a logical record of each file
      *> open in the output mode and of the most recently-read file open
      *> in the input mode."
      *>
      *> GR7 governs "the CLOSE statement WITHOUT the UNIT phrase" only,
      *> so the CLOSE ... UNIT below cannot change availability at all
      *> (Table 14's Non-unit x CLOSE UNIT cell is symbol e: "The file
      *> remains in the open mode ... and no other action takes place").
      *>
      *> The LAST line is COBOL.NET's documented DETERMINATION, not the
      *> standard's answer: once no member of the group is open the area
      *> is UNAVAILABLE (GR7) and the standard defines nothing about
      *> referencing it, so docs/CONFORMANCE.md 7 (A.1 item 24) states
      *> that the storage keeps its last content. It is pinned here so
      *> determination cannot drift silently.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "pb235sra-a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT FB ASSIGN TO "pb235sra-b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
       I-O-CONTROL.
           SAME RECORD AREA FOR FA FB.
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 A-REC PIC X(6).
       FD FB.
       01 B-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 A-ST PIC XX.
       01 B-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> Seed one record into each physical file.
           OPEN OUTPUT FA
           MOVE "AAAAAA" TO A-REC
           WRITE A-REC
           CLOSE FA
           OPEN OUTPUT FB
           MOVE "BBBBBB" TO B-REC
           WRITE B-REC
           CLOSE FB
      *> Both members open; a READ of FA puts A's record in the ONE
      *> shared area, so both record-names read it (12.4.6.4.4 GR2).
           OPEN INPUT FA
           OPEN INPUT FB
           READ FA AT END CONTINUE END-READ
           DISPLAY "READ-A=" A-ST " A=" A-REC " B=" B-REC
      *> 14.9.6.4 GR7 excludes the UNIT phrase, and Table 14's cell for
      *> it leaves the file OPEN, so nothing about the area changes.
           CLOSE FA UNIT
           DISPLAY "UNIT-A=" A-ST " A=" A-REC " B=" B-REC
      *> A successful CLOSE of FA while FB - another file-name in the
      *> same SAME RECORD AREA clause - is OPEN: GR7 keeps the area
      *> AVAILABLE, so the record just read is still there.
           CLOSE FA
           DISPLAY "CLOSE-A=" A-ST " A=" A-REC " B=" B-REC
      *> FB is now the most recently-read file open in the input mode,
      *> so the shared area holds ITS record (12.4.6.4.4 GR2).
           READ FB AT END CONTINUE END-READ
           DISPLAY "READ-B=" B-ST " A=" A-REC " B=" B-REC
      *> No member of the group is open now: GR7 makes the area
      *> UNAVAILABLE. The value below is the DETERMINATION.
           CLOSE FB
           DISPLAY "CLOSE-B=" B-ST " A=" A-REC " B=" B-REC
           STOP RUN.

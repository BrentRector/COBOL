*>--- conformance-case
*> id: pb706_organization_record_sequential
*> rules: [GR-12.4.5.10.3-3, GR-12.4.5.10.3-6, FMT-12.4.5.10.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ORGANIZATION IS RECORD SEQUENTIAL (kb/Work PB706).
      *> ISO 12.4.5.10.2 prints the ORGANIZATION clause as
      *>     [ ORGANIZATION IS ] { { LINE | RECORD } SEQUENTIAL
      *>                         | RELATIVE | INDEXED }
      *> and the rendered page (PDF p357) underlines LINE and SEQUENTIAL
      *> but NOT RECORD: an optional word (5.2.3), so RECORD SEQUENTIAL
      *> and a bare SEQUENTIAL are the same alternative.  12.4.5.10.3 GR3:
      *> "The RECORD SEQUENTIAL phrase specifies that the file
      *> organization is record sequential."  GR6: "When the ORGANIZATION
      *> clause is not specified, sequential organization with the RECORD
      *> SEQUENTIAL phrase is implied."  The phrase was a raw parse error
      *> (no grammar alternative) at every edition.
      *>
      *> Three files, one per spelling - RECORD SEQUENTIAL written out,
      *> bare SEQUENTIAL (ORGANIZATION IS omitted too), and no clause -
      *> each written two records and read back.  All three are the same
      *> organization, so every leg reads back exactly what it wrote:
      *>   R: AAAA / BBBB   S: CCCC / DDDD   T: EEEE / FFFF
      *> A leg that mis-bound RECORD SEQUENTIAL to another organization
      *> (or dropped the SELECT) would fail the READ or print nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB706ORS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "pb706r.dat"
               ORGANIZATION IS RECORD SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
           SELECT FS ASSIGN TO "pb706s.dat" SEQUENTIAL.
           SELECT FT ASSIGN TO "pb706t.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  FR.
       01  RR PIC X(4).
       FD  FS.
       01  RS PIC X(4).
       FD  FT.
       01  RT PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FR FS FT.
           WRITE RR FROM "AAAA".
           WRITE RR FROM "BBBB".
           WRITE RS FROM "CCCC".
           WRITE RS FROM "DDDD".
           WRITE RT FROM "EEEE".
           WRITE RT FROM "FFFF".
           CLOSE FR FS FT.
           OPEN INPUT FR FS FT.
           READ FR AT END DISPLAY "R EOF" END-READ.
           DISPLAY "R: " RR.
           READ FR AT END DISPLAY "R EOF" END-READ.
           DISPLAY "R: " RR.
           READ FS AT END DISPLAY "S EOF" END-READ.
           DISPLAY "S: " RS.
           READ FS AT END DISPLAY "S EOF" END-READ.
           DISPLAY "S: " RS.
           READ FT AT END DISPLAY "T EOF" END-READ.
           DISPLAY "T: " RT.
           READ FT AT END DISPLAY "T EOF" END-READ.
           DISPLAY "T: " RT.
           CLOSE FR FS FT.
           STOP RUN.

*>--- conformance-case
*> id: l1_release_same_record_area
*> rules: [GR-14.9.32.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.32.4 GR3 — RELEASE under SAME RECORD AREA: the released logical record STAYS
      *> available in the record area (the "unless … SAME RECORD AREA clause" branch of sentence 1)
      *> and is ALSO available as a record of the other file named in that clause (sentence 2).
      *> DERIVATION — every expected line follows from the rule text, nothing from the compiler.
      *>  · §12.4.6.4.4 GR2: a record-area format SAME clause makes the listed files "share a
      *>    memory area for processing the current logical record … equivalent to an implicit
      *>    redefinition of the area with records aligned on the leftmost byte position", and the
      *>    area is available "when any file connector referenced by file-name-1, file-name-2, …
      *>    is open" — hence OUTF is opened before the SORT and stays open across both phases.
      *>  · §14.9.32.4 GR3 sentence 1: because SRTF is named in a SAME RECORD AREA clause, the
      *>    release does NOT withdraw the record — SRT-REC still reads the eight bytes released
      *>    (R1-SRT / R2-SRT).
      *>  · §14.9.32.4 GR3 sentence 2: the same logical record is available as a record of the
      *>    other file referenced in that clause — OUT-REC reads the identical eight bytes
      *>    (R1-OUT / R2-OUT), leftmost-aligned, so OUT-KEY/OUT-TXT overlay SRT-KEY/SRT-TXT.
      *>  · §14.9.32.4 GR2 + §14.9.40.4 GR8 a) + §14.9.34.4 GR3: the records really were released
      *>    to the sort, so the output procedure returns them by ASCENDING SRT-KEY — "the record
      *>    containing the key data item with the lower value is returned first" — AAA then BBB.
      *>    Without this leg a compiler that made RELEASE a no-op would still pass the two
      *>    availability checks above.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RSR01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "l1rsr01-sort.dat".
           SELECT OUTF ASSIGN TO "l1rsr01-out.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME RECORD AREA FOR SRTF OUTF.
       DATA DIVISION.
       FILE SECTION.
       SD SRTF.
       01 SRT-REC.
          05 SRT-KEY PIC X(3).
          05 SRT-TXT PIC X(5).
       FD OUTF.
       01 OUT-REC.
          05 OUT-KEY PIC X(3).
          05 OUT-TXT PIC X(5).
       WORKING-STORAGE SECTION.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
      *> OUTF open => the shared record area is available to the runtime element (12.4.6.4.4 GR2).
           OPEN OUTPUT OUTF
           SORT SRTF ON ASCENDING KEY SRT-KEY
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           CLOSE OUTF
           DISPLAY "DONE"
           STOP RUN.
       FEED.
           MOVE "BBB" TO SRT-KEY
           MOVE "bbbbb" TO SRT-TXT
           RELEASE SRT-REC
           DISPLAY "R1-SRT=[" SRT-REC "]"
           DISPLAY "R1-OUT=[" OUT-REC "]"
           MOVE "AAA" TO SRT-KEY
           MOVE "aaaaa" TO SRT-TXT
           RELEASE SRT-REC
           DISPLAY "R2-SRT=[" SRT-REC "]"
           DISPLAY "R2-OUT=[" OUT-REC "]".
       DRAIN.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SRTF RECORD
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END
                       DISPLAY "SORTED=[" SRT-REC "]"
                       WRITE OUT-REC
               END-RETURN
           END-PERFORM.

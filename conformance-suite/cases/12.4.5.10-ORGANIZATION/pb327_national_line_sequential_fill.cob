      *>--- conformance-case
      *> id: pb327_national_line_sequential_fill
      *> rules: [GR-12.4.5.10.3-2, GR-14.9.30.4-15]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB327 - 14.9.30.4 GR15's NATIONAL SPACE FILL. "For a
      *> line sequential file, if the number of bytes in the record
      *> that is read is less than the minimum size specified by the
      *> record description entries for file-name-1, the portion of the
      *> record area that is to the right of the last valid character
      *> read is padded with trailing spaces. If the record-area
      *> associated with file-name-1 is specified implicitly or
      *> explicitly as alphanumeric, a trailing space is defined to be
      *> the alphanumeric space character. If the record-area
      *> associated with file-name-1 is specified implicitly or
      *> explicitly as national, a trailing space is defined to be the
      *> national space character."
      *> L-REC is an elementary national record area, so the fill is
      *> the NATIONAL space: U+0020 stored as the two bytes 00 20
      *> (13.18.60.4 GR8 / D-N1). ORD over the shared byte view is
      *> what distinguishes it - an alphanumeric byte pad would leave
      *> 20 20, i.e. 033/033, which is the character U+2020, not a
      *> space at all. The WRITE sheds the same national spaces, so
      *> the physical line is four bytes and the read re-fills them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB327LS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LF ASSIGN TO "pb327ls.dat"
              ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD LF.
       01 L-REC PIC N(4).
       01 L-BYTES PIC X(8).
       WORKING-STORAGE SECTION.
       01 W-I PIC 99.
       01 W-O PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT LF.
           MOVE N"CD" TO L-REC.
           WRITE L-REC.
           CLOSE LF.
           OPEN INPUT LF.
           READ LF AT END DISPLAY "EOF".
           DISPLAY "L=[" L-REC "]".
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 8
               MOVE FUNCTION ORD(L-BYTES(W-I:1)) TO W-O
               DISPLAY "B" W-I "=" W-O
           END-PERFORM.
           CLOSE LF.
           STOP RUN.

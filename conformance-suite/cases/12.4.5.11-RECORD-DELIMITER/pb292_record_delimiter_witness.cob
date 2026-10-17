*>--- conformance-case
*> id: pb292_record_delimiter_witness
*> rules: [SR-12.4.5.11.3-1, SR-12.4.5.11.3-2, GR-12.4.5.11.4-1, GR-12.4.5.11.4-2, GR-12.4.5.11.4-3, GR-12.4.5.11.4-4, GR-12.4.5.11.4-5, GR-12.4.5.11.4-6, FMT-12.4.5.11.2, DOC-A.1-149, DOC-A.1-150, DOC-A.1-151]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.4.5.11 RECORD DELIMITER clause - THE INERTNESS HALF of the
      *> kb/Work PB292 witness. The DECLINE half (COBOLNET1778 on both
      *> arms, every edition) cannot be asserted by a positive golden,
      *> because the corpus never reads the warning channel; it is pinned
      *> by conformance-test DocumentedNonSupportWitnessTests over THIS
      *> SAME source file, so both halves have one source of truth.
      *> WHAT THIS PROGRAM PROVES, AND WHY THE .OUT IS SPEC-DERIVED.
      *> The clause is declined whole: STANDARD-1 is Annex A.3 item 26
      *> ("The STANDARD-1 phrase of the RECORD DELIMITER clause is
      *> dependent upon a reel type of device") and 12.4.5.11.4 GR2 makes
      *> its medium a tape drive, which this implementation has none of;
      *> feature-name-1 names nothing because 12.4.5.11.3 SR2 leaves the
      *> available names to the implementor and this implementation
      *> specifies NONE (Annex A.1 item 150 is optional -
      *> docs/CONFORMANCE.md section 7). So 12.4.5.11.4 GR5 governs
      *> instead: "If the RECORD DELIMITER clause is not specified, the
      *> method used for determining the length of a variable-length
      *> record is specified by the implementor" - the 4-byte
      *> little-endian length prefix of Annex A.1 item 151.
      *> 12.4.5.11.4 GR1 is what makes the decline INERT rather than a
      *> wrong answer: "Any method used shall not be reflected in the
      *> record area or the record size used within the function, method,
      *> or program." So the lengths read back are the lengths written,
      *> whichever arm was written - and, the point of file D, whether or
      *> not the clause was written at all. That is the whole prediction.
      *> The lengths themselves come from 13.18.43.4 GR15: "after the
      *> successful execution of a READ or RETURN statement for the file,
      *> the contents of the data item referenced by data-name-1 will
      *> indicate the number of bytes in the record just read." Records
      *> of 3 and 8 bytes are written, so 03 and 08 are read back.
      *> THE FOUR FILES COVER THE GENERAL FORMAT AND GR5's ANTECEDENT.
      *> 12.4.5.11.2 is a plain required choice - on the printed page
      *> RECORD, DELIMITER and STANDARD-1 are underlined and IS is not -
      *> so the legal spellings are exactly: STANDARD-1 or feature-name-1,
      *> each with or without IS. A writes the STANDARD-1 arm with IS, B
      *> the feature-name arm with IS, C the STANDARD-1 arm with the
      *> optional word OMITTED. D writes NO clause at all, which is the
      *> only condition under which 12.4.5.11.4 GR5 speaks, and reads
      *> back identically - which is what "declined whole" means.
      *> All four files are 12.4.5.11.3 SR1-legal - "The RECORD DELIMITER
      *> clause may be specified only for variable-length records" - via
      *> the RECORD IS VARYING clause, SR1's own second NOTE case. The
      *> reference modification F1-REC(1:WS-LEN1) shows exactly the record
      *> and nothing of the area beyond it, so the prediction never
      *> depends on what a short READ leaves in the tail of the area.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB292RD1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb292rd1a.dat"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS STANDARD-1
               FILE STATUS IS WS-ST.
           SELECT F2 ASSIGN TO "pb292rd1b.dat"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS PB292-TAPE-FORMAT
               FILE STATUS IS WS-ST.
           SELECT F3 ASSIGN TO "pb292rd1c.dat"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER STANDARD-1
               FILE STATUS IS WS-ST.
           SELECT F4 ASSIGN TO "pb292rd1d.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F1
           RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
               DEPENDING ON WS-LEN1.
       01 F1-REC PIC X(8).
       FD F2
           RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
               DEPENDING ON WS-LEN2.
       01 F2-REC PIC X(8).
       FD F3
           RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
               DEPENDING ON WS-LEN3.
       01 F3-REC PIC X(8).
       FD F4
           RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
               DEPENDING ON WS-LEN4.
       01 F4-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST   PIC XX.
       01 WS-LEN1 PIC 9(2).
       01 WS-LEN2 PIC 9(2).
       01 WS-LEN3 PIC 9(2).
       01 WS-LEN4 PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F1
           MOVE 3 TO WS-LEN1
           MOVE "ABC" TO F1-REC
           WRITE F1-REC
           MOVE 8 TO WS-LEN1
           MOVE "ABCDEFGH" TO F1-REC
           WRITE F1-REC
           CLOSE F1
           OPEN OUTPUT F2
           MOVE 3 TO WS-LEN2
           MOVE "XYZ" TO F2-REC
           WRITE F2-REC
           MOVE 8 TO WS-LEN2
           MOVE "STUVWXYZ" TO F2-REC
           WRITE F2-REC
           CLOSE F2
           OPEN OUTPUT F3
           MOVE 3 TO WS-LEN3
           MOVE "LMN" TO F3-REC
           WRITE F3-REC
           MOVE 8 TO WS-LEN3
           MOVE "IJKLMNOP" TO F3-REC
           WRITE F3-REC
           CLOSE F3
           OPEN OUTPUT F4
           MOVE 3 TO WS-LEN4
           MOVE "PQR" TO F4-REC
           WRITE F4-REC
           MOVE 8 TO WS-LEN4
           MOVE "PQRSTUVW" TO F4-REC
           WRITE F4-REC
           CLOSE F4
      *> WS-LEN is forced off the written length before each READ, so
      *> what LEN= reports is 13.18.43.4 GR15's stored byte count and
      *> not the leftover MOVE that sized the WRITE.
           OPEN INPUT F1
           MOVE 99 TO WS-LEN1
           READ F1 AT END CONTINUE END-READ
           DISPLAY "A1=" WS-ST " LEN=" WS-LEN1 " [" F1-REC(1:WS-LEN1) "]"
           MOVE 99 TO WS-LEN1
           READ F1 AT END CONTINUE END-READ
           DISPLAY "A2=" WS-ST " LEN=" WS-LEN1 " [" F1-REC(1:WS-LEN1) "]"
           CLOSE F1
           OPEN INPUT F2
           MOVE 99 TO WS-LEN2
           READ F2 AT END CONTINUE END-READ
           DISPLAY "B1=" WS-ST " LEN=" WS-LEN2 " [" F2-REC(1:WS-LEN2) "]"
           MOVE 99 TO WS-LEN2
           READ F2 AT END CONTINUE END-READ
           DISPLAY "B2=" WS-ST " LEN=" WS-LEN2 " [" F2-REC(1:WS-LEN2) "]"
           CLOSE F2
           OPEN INPUT F3
           MOVE 99 TO WS-LEN3
           READ F3 AT END CONTINUE END-READ
           DISPLAY "C1=" WS-ST " LEN=" WS-LEN3 " [" F3-REC(1:WS-LEN3) "]"
           MOVE 99 TO WS-LEN3
           READ F3 AT END CONTINUE END-READ
           DISPLAY "C2=" WS-ST " LEN=" WS-LEN3 " [" F3-REC(1:WS-LEN3) "]"
           CLOSE F3
           OPEN INPUT F4
           MOVE 99 TO WS-LEN4
           READ F4 AT END CONTINUE END-READ
           DISPLAY "D1=" WS-ST " LEN=" WS-LEN4 " [" F4-REC(1:WS-LEN4) "]"
           MOVE 99 TO WS-LEN4
           READ F4 AT END CONTINUE END-READ
           DISPLAY "D2=" WS-ST " LEN=" WS-LEN4 " [" F4-REC(1:WS-LEN4) "]"
           CLOSE F4
           STOP RUN.

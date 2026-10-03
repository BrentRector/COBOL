      *> kb/Work PB1173 — A FILE SORT KEY DESCRIBED IN ONLY ONE OF THE SD'S RECORD DESCRIPTIONS IS THE SAME BYTE POSITIONS
      *> IN EVERY RECORD OF THE FILE.
      *>   cite.py --check 14.9.40.3 "The same byte positions that are referenced by a key data-name in one record
      *>     description entry are taken as the key in all records of the file." -> OK §14.9.40.3 6) e)
      *> SA is the 3-byte record the program releases; SK is described only under SB, the SD's SECOND 01, at bytes 2-3
      *> of its record. Both 01s are one area (§13.18.33.4 GR3), so SK's bytes 2-3 are the key of every record,
      *> including the SA records released here. The ordering this derives: "a30" "b10" "c20" order on bytes 2-3
      *> ("30" "10" "20"), so the sorted output is b10, c20, a30; a sort on the FIRST byte would return a30, b10, c20
      *> and a sort that took SK at offset 0 would compare bytes 1-2 ("a3" "b1" "c2") and return the same, so the
      *> expected order can only come from the same-byte-positions rule.
      *> This statement is a COBOL-85 file SORT, so the golden runs at the 85 edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173SR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1173sr.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SA PIC X(3).
       01 SB.
          05 FILLER PIC X.
          05 SK PIC X(2).
       WORKING-STORAGE SECTION.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY SK
               INPUT PROCEDURE IS LOAD
               OUTPUT PROCEDURE IS SHOW
           STOP RUN.
       LOAD SECTION.
       L1.
           MOVE "a30" TO SA
           RELEASE SA
           MOVE "b10" TO SA
           RELEASE SA
           MOVE "c20" TO SA
           RELEASE SA.
       SHOW SECTION.
       S1.
           RETURN SW AT END MOVE "Y" TO EOF-FLAG END-RETURN
           PERFORM UNTIL EOF-FLAG = "Y"
               DISPLAY SA
               RETURN SW AT END MOVE "Y" TO EOF-FLAG END-RETURN
           END-PERFORM.

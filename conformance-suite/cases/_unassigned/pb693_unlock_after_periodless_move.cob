      *>--- conformance-case
      *> id: pb693_unlock_after_periodless_move
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB693 - A WORD ISO 8.9 RESERVES MUST NOT BE ADMITTED TO
      *> THE USER-DEFINED-WORD SLOT.
      *>
      *>   8.3.2.1 rule 1: "Reserved words shall not be used as
      *>   user-defined words or system-names."  UNLOCK is in the 8.9
      *>   reserved-word list; reserved-words.json dates the
      *>   reservation to COBOL-2002 (a user word at 85).
      *>
      *> The parser's cobolWord rule IS that slot, and it admitted
      *> UNLOCK unguarded at every edition.  This whole paragraph is
      *> ONE sentence - no period until STOP RUN - so before the fix
      *> `MOVE "ZZ" TO FS UNLOCK F1` parsed as a THREE-receiver MOVE
      *> and answered COBOLNET1639 twice: conforming COBOL-2002
      *> source REJECTED, and the UNLOCK statement silently gone.
      *> The generated gate `{userWordHere("UNLOCK")}?` now withdraws
      *> the word from the slot exactly where 8.9 reserves it, so the
      *> UNLOCK statement wins the prediction.
      *>
      *> WHAT THE OUTPUT PROVES - and why it is not merely "it
      *> parsed".  Each UNLOCK is preceded by a MOVE of a SENTINEL
      *> into the FILE STATUS item, so a status of "00" can only have
      *> been written by the UNLOCK statement itself:
      *>
      *>   14.9.47.4 GR3 - "The execution of the UNLOCK statement
      *>   causes the value of the I-O status of the file connector
      *>   referenced by file-name-1 to be updated."
      *>
      *>   9.1.13.2 item 1 - "I-O status = 00.  The input-output
      *>   statement is successfully executed and no further
      *>   information is available concerning the input-output
      *>   operation."  GR1 adds that "the presence or absence of any
      *>   record locks does not affect the success of the execution
      *>   of the UNLOCK statement", so 00 is the DERIVED value on an
      *>   unlocked sequential connector - not an observed one.
      *>
      *>   14.9.47.4 GR2 - "File-name-1 shall reference a file
      *>   connector in the open mode" - hence the OPEN first.
      *>
      *> Both spellings of the 14.9.47.2 general format are exercised:
      *> the bare `UNLOCK file-name-1` and the optional RECORDS
      *> phrase.  The 85 lane of the same gate is
      *> pb693_reserved_words_as_data_names (85 dir), where UNLOCK is
      *> a legal data-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB693UNLOCKSEQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb693u1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(5).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F1
           DISPLAY "OPEN=" FS
           MOVE "AAAAA" TO R1
           WRITE R1
           DISPLAY "WRITE=" FS
           MOVE "ZZ" TO FS
           UNLOCK F1
           DISPLAY "UNLOCK=" FS
           MOVE "YY" TO FS
           UNLOCK F1 RECORDS
           DISPLAY "UNLOCKR=" FS
           CLOSE F1
           DISPLAY "CLOSE=" FS
           STOP RUN.

      *> kb/Work PB749 - THE AS-IF READ AND WRITE OF A SORT/MERGE TRANSFER
      *> SET THE I-O STATUS, UPDATE THE FILE STATUS ITEM AND RAISE
      *> EC-I-O-WARNING. 14.9.40.4 GR12 b) obtains each USING record "as
      *> if a READ statement with the NEXT phrase, the IGNORING LOCK
      *> phrase, and the AT END phrase had been executed", and GR15 b)
      *> writes each GIVING record "as if a WRITE statement without any
      *> optional phrases had been executed" (14.9.24.4 GR7 b) and GR12 b)
      *> say the same for MERGE). 9.1.13.1: "The value of the I-O status is
      *> set during the execution of a CLOSE, DELETE, OPEN, READ, REWRITE,
      *> START, UNLOCK or WRITE statement and prior to the execution of
      *> any ... applicable exception processing statements", and "Any I-O
      *> status associated with an unsuccessful completion or a nonzero
      *> successful completion is associated with an exception
      *> condition"; the exception-name for a successful completion whose
      *> status is not 00 is EC-I-O-WARNING. 12.4.5.8.4 GR1 puts the
      *> status in the program's FILE STATUS item. "These implicit
      *> functions are performed such that any applicable USE procedures
      *> are executed" - so with EC-I-O-WARNING turned on (7.3.25.4 GR4) the
      *> declarative below runs DURING the SORT, once for each as-if
      *> statement whose status is nonzero and successful, and sees the
      *> status of THAT statement in the FILE STATUS item:
      *>   A  USING F2, a LINE SEQUENTIAL file whose 7-character line is
      *>      longer than the 5-character record: the first as-if READ is
      *>      successful with status 06 (14.9.30.4 GR15), the second reads
      *>      the 2 characters left, 00.  ONE declarative run, F2=06.
      *>   B  GIVING a sequential file: every as-if WRITE is 00, so the
      *>      declarative (turned on for this statement too) must NOT run,
      *>      and the FILE STATUS item is 00 after every record and after
      *>      the statement.  No DECL line.
      *> After each statement the implicit CLOSE has set both items to 00.
      *> A leg fails if the implicit READ or WRITE neither stores its
      *> status nor offers it to the declarative (no DECL line), or runs
      *> it with a stale status (the OPEN's or the previous statement's).
       >>TURN EC-I-O-WARNING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB749SORT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb749sort.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb749sort.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS FS.
           SELECT GF ASSIGN TO "pb749sortg.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GS.
           SELECT SRT ASSIGN TO "pb749sort.tmp".
           SELECT SRG ASSIGN TO "pb749sortg.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(7).
       FD F2.
       01 R2 PIC X(5).
       FD GF.
       01 G-REC PIC X(3).
       SD SRT.
       01 SRT-REC PIC X(5).
       SD SRG.
       01 SRG-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS PIC XX VALUE "??".
       01 GS PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-WARNING.
       H-P.
           DISPLAY "DECL F2=" FS " G=" GS " " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F1
           MOVE "ABCDEFG" TO R1
           WRITE R1
           CLOSE F1
           SORT SRT ON ASCENDING KEY SRT-REC USING F2
               OUTPUT PROCEDURE SHOW.
           DISPLAY "A AFTER F2=" FS
           SORT SRG ON ASCENDING KEY SRG-REC
               INPUT PROCEDURE FEED
               GIVING GF.
           DISPLAY "B AFTER G=" GS
           STOP RUN.
       SHOW SECTION.
       SHOW-P.
           PERFORM 2 TIMES
               RETURN SRT AT END DISPLAY "END" NOT AT END
                   DISPLAY "REC [" SRT-REC "]"
               END-RETURN
           END-PERFORM.
       FEED SECTION.
       FEED-P.
           MOVE "1AA" TO SRG-REC
           RELEASE SRG-REC
           MOVE "2AA" TO SRG-REC
           RELEASE SRG-REC.

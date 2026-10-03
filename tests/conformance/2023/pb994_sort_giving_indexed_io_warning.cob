      *> kb/Work PB994 - THE EXECUTED WITNESS FOR THE SUCCESSFUL AS-IF WRITE'S EC-I-O-WARNING HOOK OF A SORT GIVING
      *> FILE (kb/Work PB749 emitted the hook; no sequential GIVING file can produce a nonzero successful status).
      *>   cite.py --check 14.9.40.4 "Each record is written as if a WRITE statement without any optional phrases had
      *>     been executed." -> OK §14.9.40.4 15) b)
      *> 9.1.13.1: "Any I-O status associated with an unsuccessful completion or a nonzero successful completion is
      *> associated with an exception condition" - for a successful completion that is EC-I-O-WARNING - and 14.9.40.4
      *> GR15's closing paragraph performs the implicit functions "such that any associated USE AFTER EXCEPTION/ERROR
      *> procedures are executed". The GIVING file IXG is INDEXED with an ALTERNATE RECORD KEY WITH DUPLICATES, so the
      *> as-if WRITE of a record whose alternate key already exists succeeds with status 02.
      *> WHY THE LEG CAN FAIL (expected values derived from the rules): the records A1x, B2x, C3y reach IXG in prime-key
      *> order (SR9: the first key SK is ASCENDING over the prime key's bytes). Only B2x repeats an alternate key
      *> (AK 'x' after A1x), so exactly ONE write has status 02 and the declarative runs ONCE, DURING the SORT, with
      *> the FILE STATUS item holding 02. A1x and C3y are 00 and run nothing. After the statement the implicit CLOSE has
      *> set the status back to 00. An implementation whose hook was never reached prints no DECL line.
       >>TURN EC-I-O-WARNING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB994IOW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRT ASSIGN TO "pb994iow.tmp".
           SELECT IXG ASSIGN TO "pb994iow.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IK
               ALTERNATE RECORD KEY IS AK WITH DUPLICATES
               FILE STATUS IS GS.
       DATA DIVISION.
       FILE SECTION.
       SD SRT.
       01 SRT-REC.
          05 SK PIC XX.
          05 SA PIC X.
       FD IXG.
       01 IXG-REC.
          05 IK PIC XX.
          05 AK PIC X.
       WORKING-STORAGE SECTION.
       01 GS PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-WARNING.
       H-P.
           DISPLAY "DECL G=" GS " " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SORT SRT ON ASCENDING KEY SK
               INPUT PROCEDURE FEED
               GIVING IXG.
           DISPLAY "AFTER G=" GS
           STOP RUN.
       FEED SECTION.
       FEED-P.
           MOVE "C3y" TO SRT-REC
           RELEASE SRT-REC
           MOVE "A1x" TO SRT-REC
           RELEASE SRT-REC
           MOVE "B2x" TO SRT-REC
           RELEASE SRT-REC.

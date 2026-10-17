*>--- conformance-case
*> id: pb981_fd_variable_length_records
*> rules: [GR-13.18.33.4-3, GR-13.18.43.4-5, GR-13.18.43.4-13]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB981 - determination D-FRA (docs/CONFORMANCE.md section 3): an FD/SD record that is a
      *> dynamic-length item or a variable-length group is an OUT-OF-LINE record. ISO 13.18.33.4 GR3 makes
      *> the FD's level-1 entries implicit redefinitions of one area; ISO 8.5.1.10.3 lets a dynamic-length
      *> item be "located elsewhere", so it shares the area at the transfer boundary: a READ makes the
      *> CURRENT RECORD available in it, and a WRITE sends its contiguous image (ISO 8.5.1.11.2).
      *> The file implies a format 2 RECORD clause (ISO 13.18.43.4 GR5 - implementor-defined).
      *> Record R = A X(3) + D dynamic (LIMIT 20) + B X(2): the fixed run is 5 characters; a record read
      *> back gives D every character beyond the fixed run, up to its maximum (the D-FRA split rule).
      *>   record 1 "ABCHELLOXY"    : A=ABC D=HELLO(5) B=XY ; R2 (the character area) = ABCH ; R3 = all 10
      *>   record 2 "SHOR"          : shorter than the fixed run - A=SHO D=""(0) B="R " ; R3 = SHOR (4)
      *>   record 3 "A LONGER LINE" : 13 - 5 = 8 characters to D - A="A L" D="ONGER LI"(8) B=NE ; R3 13
      *> This used to be refused (COBOLNET1697/1698, then COBOLNET0899), and a single-record FD with a
      *> dynamic-length member aborted the run unit at WRITE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB981VL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb981vl.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT G ASSIGN TO "pb981vlg.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT H ASSIGN TO "pb981vlh.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT K ASSIGN TO "pb981vlk.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT S ASSIGN TO "pb981vls.tmp".
       I-O-CONTROL.
           SAME RECORD AREA FOR H K.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 A PIC X(3).
          05 D PIC X DYNAMIC LENGTH LIMIT 20.
          05 B PIC X(2).
       01 R2 PIC X(4).
       01 R3 PIC X DYNAMIC LENGTH.
       FD G.
       01 GR.
          05 GA PIC 9(2).
          05 GD PIC X DYNAMIC LENGTH.
       FD H.
       01 HR PIC X(6).
       FD K.
       01 KR PIC X DYNAMIC LENGTH.
       SD S.
       01 SR.
          05 SK PIC X(2).
          05 SV PIC X DYNAMIC LENGTH LIMIT 10.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "ABC" TO A
           MOVE "HELLO" TO D
           MOVE "XY" TO B
           WRITE R
           MOVE "SHOR" TO R2
           WRITE R2
           MOVE "A LONGER LINE" TO R3
           WRITE R3
           CLOSE F
           OPEN INPUT F
           PERFORM 3 TIMES
             READ F
             DISPLAY "A=[" A "] D=[" D "] LD=" FUNCTION LENGTH(D)
                 " B=[" B "] R2=[" R2 "] R3=[" R3 "] LR3="
                 FUNCTION LENGTH(R3)
           END-PERFORM
           CLOSE F
      *> the implied format 2 frames each record at its written length (ISO 13.18.43.4 GR13 c)
           OPEN OUTPUT G
           MOVE 42 TO GA
           MOVE "VARIABLE" TO GD
           WRITE GR
           MOVE 7 TO GA
           MOVE "" TO GD
           WRITE GR
           CLOSE G
           OPEN INPUT G
           PERFORM 2 TIMES
             READ G
             DISPLAY "GA=" GA " GD=[" GD "] L=" FUNCTION LENGTH(GD)
           END-PERFORM
           CLOSE G
      *> SAME RECORD AREA (ISO 12.4.6.4.4 GR2): a READ of H makes the record available in K's KR too
           OPEN OUTPUT H
           MOVE "SHARED" TO HR
           WRITE HR
           CLOSE H
           OPEN INPUT H
           READ H
           DISPLAY "KR=[" KR "] L=" FUNCTION LENGTH(KR)
           CLOSE H
      *> an SD variable-length record: RELEASE sends the contiguous image, RETURN decomposes it
           SORT S ON ASCENDING KEY SK
               INPUT PROCEDURE IS P-IN
               OUTPUT PROCEDURE IS P-OUT
           STOP RUN.
       P-IN.
           MOVE "BB" TO SK
           MOVE "SECOND" TO SV
           RELEASE SR
           MOVE "AA" TO SK
           MOVE "FIRST-LONG" TO SV
           RELEASE SR
           MOVE "CC" TO SK
           MOVE "" TO SV
           RELEASE SR.
       P-OUT.
           PERFORM 3 TIMES
             RETURN S AT END DISPLAY "EOF" END-RETURN
             DISPLAY "SK=" SK " SV=[" SV "] L=" FUNCTION LENGTH(SV)
           END-PERFORM.

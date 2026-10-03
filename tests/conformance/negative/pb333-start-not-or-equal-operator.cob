*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 14.9.41.3 SR3 - relational-operator "is a relational operator specified in the
*> general-relation format of 8.8.4.2, Simple relation conditions, with the exception of the
*> relational operators 'IS NOT EQUAL TO' or 'IS NOT='". 8.8.4.2.2 Format 1 prints the optional NOT
*> on GREATER THAN, >, LESS THAN, <, EQUAL TO and = ONLY: IS LESS THAN OR EQUAL TO carries no NOT
*> bracket, so NOT LESS THAN OR EQUAL TO is not an alternative of the format and SR3 forbids it in
*> a START. The compiler used to accept it and fold it into '>' (kb/Work PB333). Since kb/Work PB1034 the
*> operator rule every condition shares carries the printed alternatives only, so this spelling no longer
*> parses (COBOL0001) in a START or anywhere else; the NOT EQUAL exclusion of SR3 stays START's own.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB333NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb333neg.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT IXF
           START IXF KEY IS NOT LESS THAN OR EQUAL TO IX-KEY
               INVALID KEY DISPLAY "INVALID"
           END-START
           CLOSE IXF
           STOP RUN.

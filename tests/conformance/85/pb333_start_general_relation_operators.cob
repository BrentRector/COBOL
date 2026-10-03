      *> START KEY OPERATORS ARE THE GENERAL-RELATION SET (kb/Work PB333).
      *> ISO 14.9.41.3 SR3: relational-operator "is a relational
      *> operator specified in the general-relation format of 8.8.4.2,
      *> Simple relation conditions, with the exception of the relational
      *> operators 'IS NOT EQUAL TO' or 'IS NOT='". 8.8.4.2.2 Format 1
      *> brackets [NOT] on GREATER THAN, >, LESS THAN, <, EQUAL TO and =
      *> only. The operator rule that rejects NOT >= and its spellings must
      *> still ACCEPT every alternative below; each positions exactly as
      *> 14.9.41.4 GR17 e) 1. says (the first record whose key satisfies
      *> the comparison), and READ NEXT then delivers that record.
      *> DERIVATION - the file holds AB01, AB02, CD03.
      *>  . NOT LESS THAN AB02 / NOT < AB02: first key >= AB02 -> AB02.
      *>  . GREATER THAN AB01: first key > AB01 -> AB02.
      *>  . GREATER THAN OR EQUAL TO AB05 / >= AB05: first key >= AB05
      *>    -> CD03.
      *>  . EQUAL TO CD03 -> CD03.
      *>  . 9.1.13.2 rule 1: '00' for every successful START and READ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB333GRO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb333gro.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "AB01" TO IX-KEY WRITE IX-REC
           MOVE "AB02" TO IX-KEY WRITE IX-REC
           MOVE "CD03" TO IX-KEY WRITE IX-REC
           CLOSE IXF
           OPEN INPUT IXF
           MOVE "AB02" TO IX-KEY
           START IXF KEY IS NOT LESS THAN IX-KEY
               INVALID KEY DISPLAY "S1=INVALID"
           END-START
           PERFORM SHOW
           MOVE "AB02" TO IX-KEY
           START IXF KEY NOT < IX-KEY
               INVALID KEY DISPLAY "S2=INVALID"
           END-START
           PERFORM SHOW
           MOVE "AB01" TO IX-KEY
           START IXF KEY IS GREATER THAN IX-KEY
               INVALID KEY DISPLAY "S3=INVALID"
           END-START
           PERFORM SHOW
           MOVE "AB05" TO IX-KEY
           START IXF KEY IS GREATER THAN OR EQUAL TO IX-KEY
               INVALID KEY DISPLAY "S4=INVALID"
           END-START
           PERFORM SHOW
           MOVE "AB05" TO IX-KEY
           START IXF KEY >= IX-KEY
               INVALID KEY DISPLAY "S5=INVALID"
           END-START
           PERFORM SHOW
           MOVE "CD03" TO IX-KEY
           START IXF KEY IS EQUAL TO IX-KEY
               INVALID KEY DISPLAY "S6=INVALID"
           END-START
           PERFORM SHOW
           CLOSE IXF
           STOP RUN.
       SHOW.
           READ IXF NEXT RECORD
               AT END DISPLAY "EOF"
           END-READ
           DISPLAY IX-KEY "|" ST1.

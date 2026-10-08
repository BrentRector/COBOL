      *> kb/Work PB1071 - the storage image of a pointer MEMBER of a shared storage area. ISO 1989:2023
      *> 13.18.60.4 GR23 leaves the alignment, size and representation of USAGE POINTER to the implementor and
      *> A.1 item 216 requires them documented: docs/CONFORMANCE.md DOC-A.1-216 (after GnuCOBOL, then IBM and
      *> Micro Focus) gives a data-pointer 8 storage positions holding its ADDRESS as a binary integer, the
      *> predefined address NULL being the zero address. 14.9.3.4 GR9 initializes a class-pointer item of ALLOCATEd
      *> storage to null; 14.9.39.4 GR20 moves an address by the number of bytes of SET ... UP BY n. So the positions
      *> a group MOVE copies out of a BASED area, an ALLOCATEd area or an ADDRESS-OF-taken record must be the zero
      *> address for a null member, a non-zero address for a set one, and the image must move by n for UP BY n:
      *> before the fix those positions held PB231's reserved fill (spaces), never any of these.
      *> The integer's value is the implementor's, so the program asks only the relations above: equal addresses
      *> have equal images, SET UP BY 3 differs by 3.
      *> Layout of T-PTR: T-A 1-2, T-P 3-10, T-Q(1) 11-18, T-N(1) 19, T-Q(2) 20-27, T-N(2) 28, T-Q(3) 29-36,
      *> T-N(3) 37, T-Z 38-39.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1071IMG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-PTR IS TYPEDEF STRONG.
          05 T-A PIC X(2).
          05 T-P USAGE POINTER.
          05 T-TAB OCCURS 3.
             10 T-Q USAGE POINTER.
             10 T-N PIC X(1).
          05 T-Z PIC X(2).
       01 T-RP IS TYPEDEF USAGE POINTER TO T-PTR.
       01 R TYPE T-PTR BASED.
       01 RS TYPE T-PTR.
       01 RP TYPE T-RP.
       01 WS-T PIC X(4) VALUE "ABCD".
       01 WS-U PIC X(4) VALUE "WXYZ".
       01 WS-DST PIC X(39).
       01 WS-IMG PIC X(8).
       01 WS-NUM REDEFINES WS-IMG USAGE BINARY-DOUBLE.
       01 WS-N0 USAGE BINARY-DOUBLE.
       01 WS-D PIC +9(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "ALLOCATEd BASED area"
           ALLOCATE R
           MOVE R TO WS-DST
           PERFORM SHOW
           SET T-P IN R TO ADDRESS OF WS-T
           SET T-Q IN R (2) TO ADDRESS OF WS-T
           MOVE R TO WS-DST
           PERFORM SHOW
           MOVE WS-DST(3:8) TO WS-IMG
           MOVE WS-NUM TO WS-N0
           MOVE WS-DST(20:8) TO WS-IMG
           IF WS-DST(3:8) = WS-DST(20:8)
              DISPLAY "SAME-ADDRESS=EQUAL-IMAGES"
           ELSE
              DISPLAY "SAME-ADDRESS=DIFFERENT-IMAGES"
           END-IF
           SET T-Q IN R (2) UP BY 3
           MOVE R TO WS-DST
           MOVE WS-DST(20:8) TO WS-IMG
           COMPUTE WS-D = WS-NUM - WS-N0
           DISPLAY "UP-BY-3=" WS-D
           SET T-P IN R TO ADDRESS OF WS-U
           MOVE R TO WS-DST
           IF WS-DST(3:8) = WS-DST(20:8)
              DISPLAY "OTHER-AREA=EQUAL-IMAGES"
           ELSE
              DISPLAY "OTHER-AREA=DIFFERENT-IMAGES"
           END-IF
           SET T-Q IN R (2) TO NULL
           MOVE R TO WS-DST
           PERFORM SHOW
           DISPLAY "ADDRESS-OF-taken record"
           SET RP TO ADDRESS OF RS
           MOVE RS TO WS-DST
           PERFORM SHOW
           SET T-Q IN RS (3) TO ADDRESS OF WS-U
           MOVE RS TO WS-DST
           PERFORM SHOW
           SET T-Q IN RS (3) TO NULL
           MOVE RS TO WS-DST
           PERFORM SHOW
           STOP RUN.
       SHOW.
           IF WS-DST(3:8) = LOW-VALUES
              DISPLAY "  P=NULL-IMAGE"
           ELSE
              DISPLAY "  P=ADDRESS-IMAGE"
           END-IF
           IF WS-DST(11:8) = LOW-VALUES
              DISPLAY "  Q1=NULL-IMAGE"
           ELSE
              DISPLAY "  Q1=ADDRESS-IMAGE"
           END-IF
           IF WS-DST(20:8) = LOW-VALUES
              DISPLAY "  Q2=NULL-IMAGE"
           ELSE
              DISPLAY "  Q2=ADDRESS-IMAGE"
           END-IF
           IF WS-DST(29:8) = LOW-VALUES
              DISPLAY "  Q3=NULL-IMAGE"
           ELSE
              DISPLAY "  Q3=ADDRESS-IMAGE"
           END-IF.

*>--- conformance-case
*> id: pointer_arith
*> rules: [SR-14.9.39.3-23, GR-14.9.39.4-20]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39 Format 10 (data-pointer-arithmetic) GR20: SET p UP|DOWN BY n increments/decrements the
      *> pointer's address by n BYTES. Here P is pointed at a 10-byte buffer; UP/DOWN BY moves it within the
      *> buffer, and a BASED 1-byte item rebased onto P reads the byte at the current address.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PTRARITH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BUF  PIC X(10) VALUE "ABCDEFGHIJ".
       01 P    USAGE POINTER.
       01 C    PIC X(1) BASED.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF BUF.
           SET ADDRESS OF C TO P.
           DISPLAY "C0=" C.
           SET P UP BY 4.
           SET ADDRESS OF C TO P.
           DISPLAY "C4=" C.
           SET P DOWN BY 2.
           SET ADDRESS OF C TO P.
           DISPLAY "C2=" C.
           STOP RUN.

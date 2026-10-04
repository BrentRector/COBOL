      *> reject-at: 2002 2014 2023
      *> kb/Work PB816. ISO 14.9.39.3 SR24 (Format 10): "Identifier-9 shall not be a data-pointer restricted to a type
      *> described with the STRONG phrase." P1 is restricted to REC-T, a STRONG TYPEDEF, so SET P1 UP BY and SET P1 DOWN BY
      *> are each refused COBOLNET0869 (the message names SR24); a list that includes it is refused once for it. P2 (an
      *> unrestricted pointer) and PW (restricted to the WEAK type REC-W) are legal receivers, and are the positive golden
      *> 2002/pb665_restricted_pointer_conforming. The record is addressed through the Format-7 sibling, which already
      *> screens the same restriction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB816N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 R-A PIC X(4).
       01 PTR-T IS TYPEDEF USAGE POINTER TO REC-T.
       01 REC-W IS TYPEDEF.
          05 W-A PIC X(4).
       01 PTR-W IS TYPEDEF USAGE POINTER TO REC-W.
       01 P1 TYPE PTR-T.
       01 PW TYPE PTR-W.
       01 HOLDER TYPE REC-T.
       01 P2 USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET P1 TO ADDRESS OF HOLDER
           SET P1 UP BY 4
           SET P1 DOWN BY 2
           SET P2 P1 UP BY 1
           SET P2 UP BY 1
           SET PW UP BY 1
           STOP RUN.

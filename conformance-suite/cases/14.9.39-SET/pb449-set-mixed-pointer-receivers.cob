*>--- conformance-case
*> id: pb449-set-mixed-pointer-receivers
*> rules: [SR-14.9.39.3-17, SR-14.9.39.3-23]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR23 - "Identifier-9 shall be of category data-pointer." Format 10's receiving brace is
      *> { identifier-9 } ..., one or more operands of that ONE category, so a data-pointer written beside an
      *> integer data item leaves WS-N in violation of SR23 whichever operand comes first.
      *> ⛔ THE RECEIVER ORDER IS THE POINT (kb/Work PB449). SET P1 WS-N UP BY 4 drew this diagnostic and
      *> SET WS-N P1 UP BY 4 - the SAME two operands - compiled clean and aborted at run time, because the format
      *> was sniffed from receivers[0] alone. Source order is a variable in no rule of 14.9.39, so this fixture
      *> writes the POINTER SECOND: the order that used to escape every screen.
      *> Rejected from 2002: USAGE POINTER and Format 10 are both COBOL-2002 introductions, so below 2002 the
      *> declaration itself is refused with the edition-band code instead and the SR23 verdict is not the subject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB449N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P1 USAGE POINTER.
       01 WS-N PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-N P1 UP BY 4
           STOP RUN.

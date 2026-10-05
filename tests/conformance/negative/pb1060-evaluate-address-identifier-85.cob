      *> reject-at: 85
      *> kb/Work PB1060 - an ADDRESS-IDENTIFIER as an EVALUATE selection subject and object (ISO/IEC 1989:2023
      *> 8.4.3.1.2 identifier Format 9; 14.9.13.3 SR7 a) is legal from COBOL-2002 (positive
      *> 2002/pb1060_address_identifier_evaluate_argument). The address-identifier is a 2002 introduction, so at
      *> --std 85 the ONE introduction gate (VersionConformancePass.VisitAddressIdentifier) names the edition
      *> (COBOLNET0900) - the EVALUATE surface reaches it through the same addressIdentifier rule as the relation
      *> operand, rather than failing to parse.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1060N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X.
       01 B PIC X.
       PROCEDURE DIVISION.
           EVALUATE ADDRESS OF A
             WHEN ADDRESS OF B DISPLAY "B"
             WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.

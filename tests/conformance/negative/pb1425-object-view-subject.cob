      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 - ISO 8.4.3.5.3 SR1: "Identifier-1 shall be of class object; the predefined object
      *>   references SUPER and NULL shall not be specified." N is a numeric item, not an object reference,
      *>   and NULL is named by the rule: both object-views are refused by SR1 (COBOLNET2870), never by the
      *>   reserved-word diagnostic AS used to draw.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425W1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 N PIC 9.
       PROCEDURE DIVISION.
           SET U TO N AS UNIVERSAL.
           SET U TO NULL AS UNIVERSAL.
           STOP RUN.
       END PROGRAM PB1425W1.

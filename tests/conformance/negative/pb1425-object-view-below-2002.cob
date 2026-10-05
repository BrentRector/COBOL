      *> reject-at: 85
      *> kb/Work PB1425 - the object-view (ISO 8.4.3.1.2 Format 5, 8.4.3.5) is object-orientation surface
      *> introduced with COBOL-2002 (constructs.json object-view-2002: A.4.10 does not list it among the
      *> optional OO elements, Annex E's 2014->2023 delta does not list it). The program uses no other
      *> OO construct, so the COBOLNET0900 below names the object-view itself; the positive twin is
      *> tests/conformance/2002/pb1425_object_view.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425W0.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       01 Y PIC 9.
       PROCEDURE DIVISION.
           SET X TO Y AS UNIVERSAL.
           STOP RUN.
       END PROGRAM PB1425W0.

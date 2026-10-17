*>--- conformance-case
*> id: pb889_type_to_optional_word
*> rules: [SR-13.18.49.3-9, SR-13.18.57.3-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB889 - the TYPE clause's Format 1 is printed "TYPE TO type-name-1" (ISO 13.18.57.2, rendered
      *> PDF page 524) with TYPE underlined and TO not: 5.2.3 makes TO an OPTIONAL word, so "TYPE TO T" and
      *> "TYPE T" are the same clause. The grammar used to spell the optional word as IS and rejected the
      *> standard's own spelling (COBOL0001 "unexpected 'TO'").
      *>
      *> It also pins the LEGAL side of 13.18.57.3 SR5 / 13.18.49.3 SR9: a group with NO GROUP-USAGE, SIGN
      *> or USAGE clause may hold TYPE and SAME AS subordinates. The negatives
      *> tests/conformance/negative/pb889-group-usage-over-type / -over-same-as pin the illegal side.
      *>
      *> EXPECTED VALUES, DERIVED. 13.18.57.4 GR1: the effect is "as though the data description identified
      *> by type-name-1 had been coded in place of the TYPE clause", so both spellings give a group over
      *> PIC N(4) - 15.50.4 r3 (not a national GROUP: its superordinate has no GROUP-USAGE) counts it in
      *> alphanumeric character positions of a two-byte national item = 8. The SAME AS copy of SRC2 PIC N(4)
      *> is an elementary national item, 15.50.4 r2 = 4 national positions - and inside the alphanumeric group
      *> OUTER those occupy 8 alphanumeric positions like the others (the two-byte national character, 13.18.60.4
      *> GR8, pinned by determination D-N1), so OUTER = 8 + 8 + 8 = 24.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB889TYPETO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TSRC TYPEDEF.
          05 TN PIC N(4).
       01 SRC2 PIC N(4).
       01 OUTER.
          02 WITH-TO TYPE TO TSRC.
          02 WITHOUT-TO TYPE TSRC.
          02 COPIED SAME AS SRC2.
       PROCEDURE DIVISION.
           MOVE N"ABCD" TO TN OF WITH-TO
           MOVE N"EFGH" TO TN OF WITHOUT-TO
           MOVE N"IJKL" TO COPIED
           DISPLAY "LEN-TO=" FUNCTION LENGTH(WITH-TO)
           DISPLAY "LEN-NOTO=" FUNCTION LENGTH(WITHOUT-TO)
           DISPLAY "LEN-SAME=" FUNCTION LENGTH(COPIED)
           DISPLAY "LEN-OUTER=" FUNCTION LENGTH(OUTER)
           DISPLAY "V=" TN OF WITH-TO TN OF WITHOUT-TO COPIED
           STOP RUN.

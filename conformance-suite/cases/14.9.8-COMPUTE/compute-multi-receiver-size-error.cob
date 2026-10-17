      *>--- conformance-case
      *> id: compute-multi-receiver-size-error
      *> rules: [GR-14.9.8.4-2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> ISO 14.9.8.4 GR 2: the value is stored "in conformance with
      *> the specifications in ... 14.7.5, SIZE ERROR phrase and size
      *> error condition, into each data item referenced by
      *> identifier-1". 14.7.5, SIZE ERROR phrase specified, rule 2:
      *> "If the absolute value of the result of the arithmetic
      *> operation exceeds the maximum value allowed for any resultant
      *> identifier, the content of that resultant identifier is not
      *> changed ... The values of resultant identifiers for which the
      *> size error condition did not occur are the same as they would
      *> have been if the size error condition had not existed";
      *> rule 3: control then goes to the SIZE ERROR imperative.
      *> So 124 leaves R-SMALL (PIC 99) at 7 and stores 124 in R-LARGE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSCMRSE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R-SMALL PIC 99 VALUE 7.
       01 R-LARGE PIC 9(4) VALUE 0.
       01 E-SMALL PIC Z9.
       01 E-LARGE PIC ZZZ9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R-SMALL R-LARGE = 123 + 1
               ON SIZE ERROR DISPLAY "SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "NO SIZE ERROR"
           END-COMPUTE
           MOVE R-SMALL TO E-SMALL
           MOVE R-LARGE TO E-LARGE
           DISPLAY "R-SMALL=" E-SMALL
           DISPLAY "R-LARGE=" E-LARGE
           STOP RUN.

      *>--- conformance-case
      *> id: pb157_compute_multi_sizeerr
      *> rules: [GR-14.9.8.4-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB157MS.
      *> kb/Work PB157 - 14.9.8.4 GR2 via 14.7.5's storing rule: ONE
      *> value stored into EACH receiver left-to-right; a receiver whose
      *> store overflows is left UNCHANGED while the others take the
      *> value, and the ON SIZE ERROR imperative runs ONCE after the
      *> stores. The only prior multi-receiver size-error golden (ca5b)
      *> failed BOTH receivers, so the MIXED direction - the partial
      *> commit - had never been observed.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(6) VALUE 0.
       01 SML PIC 9(2) VALUE 99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE BIG SML = 123456
               ON SIZE ERROR DISPLAY "SE"
               NOT ON SIZE ERROR DISPLAY "OK"
           END-COMPUTE
           DISPLAY "BIG=" BIG " SML=" SML
           STOP RUN.

      *>--- conformance-case
      *> id: copy-library-text
      *> rules: [GR-7.2.3.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> files: [CSCOPY01.cpy]
      *> source-format: fixed
      *>---
      *> ISO 7.2.3.4 GR 1: "Text-name-1 or literal-1 identifies the
      *> library text to be processed by the COPY statement." The text
      *> named CSCOPY01, declared beside this test, describes W-GREETING
      *> with its VALUE; displaying it shows the library text was
      *> processed. Locating library text is the implementor's (GR 3),
      *> so the adapter supplies the test's directory as the library.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSCOPYL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY CSCOPY01.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "W-GREETING=" W-GREETING
           STOP RUN.

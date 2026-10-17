*>--- conformance-case
*> id: cc_in_copy
*> rules: []
*> accept: [2023]
*> files: [cc_in_copy_data.cpy]
*> source-format: free
*> portable: unverified
*>---
       >>DEFINE XMODE AS 2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CCICOPY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY cc_in_copy_data.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY W-VAL.
           STOP RUN.

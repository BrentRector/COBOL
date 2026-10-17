*>--- conformance-case
*> id: pb2720_paragraph_header_mid_line
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       >>SOURCE FORMAT FREE
*> kb/Work PB2720 - a paragraph header written after a sentence on the
*> same line. ISO 14.4.3: "A paragraph consists of a paragraph-name
*> followed by a separator period and by zero, one, or more successive
*> sentences"; 6.4.1: free-form text "may be written anywhere on a
*> line". Section and paragraph headers agree: every header below
*> follows a separator period mid-line, in free form and in fixed form.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB2720PH.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9 VALUE 0.
PROCEDURE DIVISION.
S1 SECTION. P1. DISPLAY "P1". PERFORM P2. PERFORM P3 OF S2 THRU P4 OF S2. PERFORM S3. STOP RUN. P2. DISPLAY "P2".
S2 SECTION. P3. P4. ADD 1 TO W-N. DISPLAY "P4 " W-N.
>>SOURCE FORMAT FIXED
       S3 SECTION. P5. DISPLAY "P5". P6. DISPLAY "P6".

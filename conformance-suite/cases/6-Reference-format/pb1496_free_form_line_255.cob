*>--- conformance-case
*> id: pb1496_free_form_line_255
*> rules: [SR-6.1-3, DOC-A.1-157]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 6.1 3) a): "The number of character positions on a line may vary from
*> line to line, ranging from a minimum of 0 to a maximum of 255." The DISPLAY line below has
*> EXACTLY 255 positions - accepted; negative/pb1496_free_form_line_256 has 256.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1496OK.
PROCEDURE DIVISION.
DISPLAY "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx".
STOP RUN.

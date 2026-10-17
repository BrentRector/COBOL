*>--- conformance-case
*> id: pb1006_source_format_in_false_path
*> rules: [GR-7.3.16.4-2, GR-7.3.16.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1006 (DISCHARGED) - a SOURCE FORMAT directive written in the
      *> FALSE path of an >>IF is still PROCESSED, and the IF directive includes
      *> and omits its two texts.
      *>
      *> THE RULES:
      *>   7.2.1 Step 1 - "Lines that appear in the false path of an IF or
      *>     EVALUATE directive, including library text identified in COPY
      *>     statements, may be omitted from the expanded compilation group.
      *>     SOURCE FORMAT directives in the false path shall be processed to
      *>     correctly interpret input lines."
      *>   7.3.16.4 GR2 - condition TRUE: "all lines of text-1 are included in
      *>     the resultant text and all lines of text-2 are omitted".
      *>   7.3.16.4 GR3 - condition FALSE: text-2 included, text-1 omitted.
      *>   7.3.24.3 GR1 - the text following a SOURCE FORMAT directive is
      *>     treated as fixed or free form as it specifies.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES:
      *>   L1 TEXT-1   V = 1 is TRUE (GR2).
      *>   L2 TEXT-2   V = 2 is FALSE (GR3).
      *>   L3 FREE     the >>SOURCE FREE sits in the false path of >>IF V = 2,
      *>               and Step 1 processes it anyway, so the DISPLAY written
      *>               from column 1 after >>END-IF is read in FREE form. (Read
      *>               in fixed form, columns 1-6 would be its sequence area and
      *>               the program would not compile.)
      *> Directives sit at COLUMN 8 in the fixed-form text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1006SF.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>IF V = 1
           DISPLAY "L1 TEXT-1"
       >>ELSE
           DISPLAY "L1 TEXT-2"
       >>END-IF
       >>IF V = 2
           DISPLAY "L2 TEXT-1"
       >>ELSE
           DISPLAY "L2 TEXT-2"
       >>END-IF
       >>IF V = 2
       >>SOURCE FREE
       >>END-IF
DISPLAY "L3 FREE"
>>SOURCE FIXED
           STOP RUN.

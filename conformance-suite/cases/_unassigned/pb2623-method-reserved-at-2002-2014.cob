*>--- conformance-case
*> id: pb2623-method-reserved-at-2002-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB2623 - METHOD is a reserved word at COBOL-2002 and COBOL-2014 (the GnuCOBOL per-standard lists the
*> table is derived from) and was dropped from the COBOL-2023 §8.9 list (cite.py --check 8.9 "METHOD-ID" -> OK, the
*> list names METHOD-ID and not METHOD), so §8.3.2.1 rule 1 ("Reserved words shall not be used as user-defined words
*> or system-names", cite.py --check 8.3.2.1 -> OK) refuses it as a data name below 2023. The positive twin is
*> conformance/2023/pb2623_method_is_a_user_word_at_2023.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB2623N.
DATA DIVISION.
WORKING-STORAGE SECTION.
01  METHOD PIC X(5) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY METHOD.
    STOP RUN.

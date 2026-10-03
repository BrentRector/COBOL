      *> reject-at: 85 2002 2014 2023
      *> ISO §15.67.3 r1: "Argument-1 shall be an alphanumeric or national literal or an alphanumeric or
      *> national data item whose content has one of the following two formats" (cite.py --check 15.67.3
      *> "Argument-1 shall be an alphanumeric or national literal or an alphanumeric or national data item"
      *> -> OK, §15.67.3 rule 1).
      *>
      *> THE WORDING IS CATEGORY, NOT CLASS. §8.5.2.1's closing sentence: "Use of the name of a data class or
      *> data category in the rules of COBOL refers to the category unless class is specifically indicated"
      *> (cite.py --check 8.5.2.1 OK), and §8.5.2.3 4) names the item it lists - an elementary item described as
      *> alphanumeric by its PICTURE - "an alphanumeric data item" (cite.py --check 8.5.2.3 "Such an item is
      *> referred to as an alphanumeric data item" -> OK). A PIC ZZ9 item is category NUMERIC-EDITED: Table 2 puts
      *> it in class alphanumeric, but r1 does not say class (TEST-NUMVAL's §15.93.3 r1 does: "a data item of
      *> class alphanumeric or national", and l1_test_numval_f_class_screen pins the admission there).
      *>
      *> kb/Work PB658: the generic string screen ADMITTED a numeric-edited item while a bespoke binder arm
      *> rejected it for NUMVAL-C alone, so NUMVAL and NUMVAL-F (the same category wording) screened only the
      *> class. One mechanism now owns it: the CharacterCategory predicate on the schema row.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB658NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED PIC ZZ9.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           MOVE 25 TO ED
           COMPUTE R = FUNCTION NUMVAL(ED)
           STOP RUN.

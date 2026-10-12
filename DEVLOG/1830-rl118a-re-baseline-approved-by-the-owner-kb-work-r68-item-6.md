## Entry 1830 — 2026-09-30 22:02 PDT — RL118A re-baseline approved by the owner (kb/Work R68 item 6)

The I-O bundle of train w0930 makes a permanent '3x' status remain in effect until a completed CLOSE (section 9.1.13.1; DOC-A.1-105). NIST RL118A writes a 100th record whose key lands as key 0 ('34', section 14.9.51.4 GR29 b)), so its duplicate-key WRITE (REL-TEST-2) now answers '34' instead of the CCVS's '22', which assumes no persistence. The bundle re-classified it `divergent` with a `CCVS-DEFECT` note and re-baselined its golden; the lander's reviewer judged that licensed by the standard. The owner, shown it as a veto point, answered "RL118A - agreed". Recorded as R68 item 6 so no later session re-asks. No code or test changed in this entry.


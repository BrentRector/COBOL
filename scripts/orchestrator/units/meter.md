UNIT: meter. Skip the session-start skill: this unit reads one page and changes nothing in the repository.

1. Read the weekly and session usage with Claude in Chrome, as the memory note reference_quota_meter_artifact says
   (claude.ai settings, usage page): the weekly %, the current session %, and when the session resets.
   The page is the meter of whichever account the browser is signed into, and two accounts run this loop (kb/Work
   PB2478): the page's signed-in email must equal `python scripts/orchestrator/account.py --field email` (this
   session's account). If it differs, `outcome: failed` naming both; never record another account's meter.
2. Record it: `python scripts/orchestrator/budget.py --record --weekly <W> --session <S> --session-reset <ISO time
   with zone>` (it stamps the reading with this session's account); then run `python scripts/orchestrator/budget.py`
   and quote its line in the summary.
3. Write the handoff with `meter_reading` and `next_unit` null. If the page cannot be read, `outcome: failed` and say
   why; never invent a reading.

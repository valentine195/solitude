# Task journal

## Feature request

“Spec out a task journal feature, including a system for tasks to insert themselves (event?)”

## Human context

“Let’s see some mockups for the task journal using [$solitude-ui](/Users/jeremyvalentine/SOLITUDE/.agents/skills/solitude-ui/SKILL.md)”

## Workflow scope

Begin the QRSPI workflow for this feature. The earlier standalone specification and generated mockups are proposals; their defaults and visual details have not been explicitly approved as requirements. Capture product clarifications during discovery before defining the formal behavioral delta.

## Clarifications

1. What should players be able to see and do in the journal, and should discovering entries happen automatically, through player action, or both?

   Answer: Both

2. What stages should a task have from discovery to completion, and should completed, failed, or abandoned tasks remain visible?

   Answer: This could vary by task. Failed should probably go in the “completed” section, with abandoned just being removed.

3. Which gameplay occurrences should add or update tasks in this feature’s initial scope, and what playable example would demonstrate that behavior satisfactorily?

   Answer: Initial scope could maybe be the AI, or just seeing a broken scrubber

4. Should the journal show title, location, status, condition, and next step/result, with no gameplay actions? Or should players also be able to abandon or pin tasks?

   Answer: Yes, pin

5. Which example should we deliver first: discovering a broken scrubber, receiving a task from the AI, or both? I recommend the scrubber.

   Answer: Scrubber

6. For the scrubber, does discover fault → replace fuse → restart → wait for recovery → completed work? Should the first example also demonstrate failure or abandonment?

   Answer: Yes, no failure or abandonment

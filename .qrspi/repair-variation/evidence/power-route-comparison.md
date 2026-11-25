# Scrubber power route comparison

The tested patrol variation did not produce a reason to switch routes. The Engineer exited about 1.1 seconds earlier with the jumper in both versions. With SEC-0 present, both routes were visible to the rover for effectively the entire run and took two stuns. With SEC-0 elsewhere, both routes had zero stuns. The breaker preserved the lights and left the jumper available, but neither benefit was required to complete the room.

## Setup

Four scripted Unity PlayMode journeys, each repeated twice using synthesized keyboard input. Every run started at the authored Engineer / Loss of Power spawn. Movement used the live keyboard bindings, and each run interacted with the authored controls, waited for airflow to recover, and walked through the return door. The route was chosen in advance. Both scenarios used the same room, equipment, spawn and lighting behavior. In the patrol-elsewhere scenario only, the fixture deactivated SEC-0 in the isolated scene to model an empty maintenance room. The experiment used one route per repair and measured elapsed in-game time. It did not sample other patrol timings, alternate routes, player discovery or subjective fun.

| Patrol | Repair | Time to airflow | Time to exit | SEC-0 shots | Stuns | Exit light | Jumper at exit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Present | Breaker | 2.68 s | 8.79 s | 2 | 2 | On | At pickup |
| Present | Jumper | 1.57–1.58 s | 7.68–7.69 s | 3 | 2 | Off | Installed |
| Elsewhere | Breaker | 2.67–2.68 s | 8.78–8.79 s | 0 | 0 | On | At pickup |
| Elsewhere | Jumper | 1.58 s | 7.69 s | 0 | 0 | Off | Installed |

## What to change before procedural variation

Give the service panel an approach behind cover so the jumper can avoid the patrol, then replay both variants. If the clear-room choice still favors bypass, give the lit exit or the available jumper a concrete payoff. One option is a second jumper socket just beyond the return door that opens an optional supply cache. The breaker saves the player a return trip to recover the tool. A quick bypass remains attractive when the patrol makes the breaker trip costly. SEC-0 currently uses sight range, facing and geometry for detection. Switching off the corridor light does not directly change that calculation.

## Evidence

Unity 6000.6.4f1. Source HEAD `010b0009d63802a1380d14d49d693b995581f598` plus Assets diff SHA-256 `a4c2570618ffd4417b0dc982a30d6d2f68d7988a8dccc8a9e9009bdfc0c6ef06`. The changed assets and SEC-0 controller matched the isolated project. PlayMode filter `SOLITUDE.Tests.RepairRouteComparisonTests`. The initial fixture attempt stopped on feedback timing/assertion diagnostics and was excluded. Both complete runs passed all four cases. [Run 1 XML](run-1.xml) · [Run 2 XML](run-2.xml) · [Fixture](RepairRouteComparisonTests.cs).

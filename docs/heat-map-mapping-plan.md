# Heat Map Mapping Plan

This document turns the current exercise library into a practical weighted heat-map plan.

It is based on:
- the app's current exercise list in `WorkoutTracker.App/Resources/Raw/exercises.json`
- ExRx exercise profiles for pull-ups, bench press, dips, and rear-delt raises
- ACE exercise library body-part taxonomy and exercise entries for hammer curls and squat/lunge/carry patterns

Important note:
- The weights below are an implementation-oriented inference, not direct EMG percentages.
- They are meant to answer: "How much should this exercise heat each region relative to the others in this app?"
- Primary muscles are usually around `1.00`
- Secondary muscles usually land around `0.15` to `0.60`

## Source Notes

Primary sources used:
- ExRx pull-up: https://exrx.net/WeightExercises/LatissimusDorsi/STPullup
- ExRx bench press: https://exrx.net/WeightExercises/PectoralSternal/BBBenchPress.html
- ExRx triceps dip: https://exrx.net/WeightExercises/Triceps/CBTriDip
- ExRx rear lateral raise: https://exrx.net/WeightExercises/DeltoidPosterior/CBRearLateralRaise
- ACE hammer curl library entry: https://www.acefitness.org/resources/everyone/exercise-library/10/hammer-curl/
- ACE exercise taxonomy and exercise library:
  - https://www.acefitness.org/resources/everyone/exercise-library/
  - https://www.acefitness.org/resources/everyone/exercise-library/?srsltid=AfmBOooNWxnLkO3_5OdO0maiPXJg9unYjp0nKA4I3MVfh7c64jT7J_3U
  - https://www.acefitness.org/resources/everyone/exercise-library/?srsltid=AfmBOorngzF-W5oEqlvExGzcHyClRp0cjfO0DYDUqDR_7l80cmyux45h
  - https://www.acefitness.org/resources/everyone/exercise-library/?srsltid=AfmBOop3c50rSHgUwHRH40G-VJaiaT_nV6lsQX6dFWtT9D28KTkRMYn2
- Additional hammer-curl anatomy context:
  - https://www.healthline.com/health/fitness/hammer-curls

## Current Heat Regions

Already in the app:
- `FrontShoulders`
- `FrontChest`
- `FrontBiceps`
- `FrontTriceps`
- `FrontAbs`
- `FrontQuads`
- `BackShoulders`
- `BackTriceps`
- `BackLats`
- `BackLowerBack`
- `BackGlutes`
- `BackHamstrings`
- `BackCalves`

## Recommended New Heat Regions

These are the most worthwhile additions before drawing more overlays:

1. `BackTraps`
Reason:
- shrugs, carries, rows, face pulls, rear-delt work, and many deadlift patterns currently have no good "upper-back" destination

2. `BackRhomboids`
Reason:
- rows, face pulls, and pull-up variations hit scapular retraction strongly, but that detail is currently lost inside `BackShoulders` or `BackLats`

3. `FrontForearms`
Reason:
- hammer curls, reverse curls, farmer carries, suitcase carries, and dumbbell-heavy work all meaningfully involve forearm flexors and brachioradialis

Minimal next overlay asset set:
- `body_back_traps.webp`
- `body_back_rhomboids.webp`
- `body_front_forearms.webp`

If you also want matching base/body planning names for future expansion:
- `body_back_traps_active.webp`
- `body_back_rhomboids_active.webp`
- `body_front_forearms_active.webp`

Recommended image folder:
- `WorkoutTracker.App/Resources/Images/`

## Weighted Mapping Table

Format:
- `Exercise(s) -> Region:Weight, Region:Weight`

### Back / Pulling

Deadlift
- `BackGlutes:0.90, BackHamstrings:0.80, BackLowerBack:0.65, FrontQuads:0.35, BackLats:0.20`

Trap Bar Deadlift
- `BackGlutes:0.85, BackHamstrings:0.70, FrontQuads:0.45, BackLowerBack:0.45, BackLats:0.15`

Romanian Deadlift
- `BackHamstrings:1.00, BackGlutes:0.65, BackLowerBack:0.45, BackLats:0.15`

Stiff-Leg Deadlift
- `BackHamstrings:1.00, BackGlutes:0.65, BackLowerBack:0.45, BackLats:0.15`

Hip Hinge Drill
- `BackLowerBack:0.50, BackHamstrings:0.35, BackGlutes:0.35`

Pull-Up
- `BackLats:1.00, FrontBiceps:0.45, BackShoulders:0.20, BackRhomboids:0.20`

Assisted Pull-Up
- `BackLats:1.00, FrontBiceps:0.45, BackShoulders:0.20, BackRhomboids:0.20`

Weighted Pull-Up
- `BackLats:1.00, FrontBiceps:0.45, BackShoulders:0.20, BackRhomboids:0.20`

Lat Pulldown
- `BackLats:1.00, FrontBiceps:0.40, BackShoulders:0.15, BackRhomboids:0.15`

Neutral-Grip Lat Pulldown
- `BackLats:1.00, FrontBiceps:0.45, BackShoulders:0.15, BackRhomboids:0.15`

Bent-Over Row
- `BackLats:0.90, BackRhomboids:0.55, BackShoulders:0.40, FrontBiceps:0.35, BackLowerBack:0.20`

Seated Row
- `BackLats:0.85, BackRhomboids:0.55, BackShoulders:0.35, FrontBiceps:0.30`

Seated Cable Row
- `BackLats:0.85, BackRhomboids:0.55, BackShoulders:0.35, FrontBiceps:0.30`

Chest-Supported Row
- `BackLats:0.85, BackRhomboids:0.55, BackShoulders:0.40, FrontBiceps:0.30`

Single-Arm Dumbbell Row
- `BackLats:0.90, BackRhomboids:0.50, BackShoulders:0.35, FrontBiceps:0.30`

Resistance Band Row
- `BackLats:0.80, BackRhomboids:0.55, BackShoulders:0.35, FrontBiceps:0.25`

T-Bar Row
- `BackLats:0.90, BackRhomboids:0.60, BackShoulders:0.40, FrontBiceps:0.30`

Face Pulls
- `BackShoulders:1.00, BackRhomboids:0.45, BackTraps:0.40, BackLats:0.15`

Face Pull
- `BackShoulders:1.00, BackRhomboids:0.45, BackTraps:0.40, BackLats:0.15`

Reverse Pec Deck Fly
- `BackShoulders:1.00, BackRhomboids:0.45, BackTraps:0.30`

Rear Delt Fly
- `BackShoulders:1.00, BackRhomboids:0.40, BackTraps:0.25`

Rear Delt Cable Fly
- `BackShoulders:1.00, BackRhomboids:0.40, BackTraps:0.25`

### Biceps / Arms

Barbell Curl
- `FrontBiceps:1.00, FrontForearms:0.20`

Dumbbell Curl
- `FrontBiceps:1.00, FrontForearms:0.20`

Preacher Curl
- `FrontBiceps:1.00, FrontForearms:0.10`

Concentration Curl
- `FrontBiceps:1.00, FrontForearms:0.10`

Incline Dumbbell Curl
- `FrontBiceps:1.00, FrontForearms:0.15`

Cable Curl
- `FrontBiceps:1.00, FrontForearms:0.15`

EZ-Bar Curl
- `FrontBiceps:1.00, FrontForearms:0.15`

Hammer Curl
- `FrontForearms:1.00, FrontBiceps:0.70`

Reverse Curl
- `FrontForearms:1.00, FrontBiceps:0.55`

### Chest / Pushing

Bench Press
- `FrontChest:1.00, FrontTriceps:0.40, FrontShoulders:0.35`

Barbell Bench Press
- `FrontChest:1.00, FrontTriceps:0.40, FrontShoulders:0.35`

Decline Bench Press
- `FrontChest:1.00, FrontTriceps:0.35, FrontShoulders:0.25`

Incline Bench Press
- `FrontChest:1.00, FrontShoulders:0.45, FrontTriceps:0.35`

Incline Dumbbell Press
- `FrontChest:1.00, FrontShoulders:0.45, FrontTriceps:0.35`

Dumbbell Bench Press
- `FrontChest:1.00, FrontTriceps:0.40, FrontShoulders:0.35`

Dumbbell Floor Press
- `FrontChest:0.90, FrontTriceps:0.45, FrontShoulders:0.25`

Machine Chest Press
- `FrontChest:1.00, FrontTriceps:0.35, FrontShoulders:0.30`

Chest Press Machine
- `FrontChest:1.00, FrontTriceps:0.35, FrontShoulders:0.30`

Machine Incline Press
- `FrontChest:1.00, FrontShoulders:0.45, FrontTriceps:0.30`

Dumbbell Fly
- `FrontChest:1.00, FrontShoulders:0.15`

Pec Deck Fly
- `FrontChest:1.00, FrontShoulders:0.10`

Cable Crossover
- `FrontChest:1.00, FrontShoulders:0.10`

Push-Up
- `FrontChest:0.85, FrontTriceps:0.40, FrontShoulders:0.30, FrontAbs:0.15`

Incline Push-Up
- `FrontChest:0.80, FrontTriceps:0.35, FrontShoulders:0.25, FrontAbs:0.10`

Wall Push-Up
- `FrontChest:0.70, FrontTriceps:0.25, FrontShoulders:0.20, FrontAbs:0.05`

Elevated Push-Up
- `FrontChest:0.80, FrontTriceps:0.35, FrontShoulders:0.25, FrontAbs:0.10`

Chest Dip
- `FrontChest:0.75, FrontTriceps:0.50, BackTriceps:0.55, FrontShoulders:0.35`

Close-Grip Bench Press
- `FrontTriceps:0.70, BackTriceps:0.80, FrontChest:0.55, FrontShoulders:0.20`

Diamond Push-Ups
- `FrontTriceps:0.65, BackTriceps:0.75, FrontChest:0.45, FrontShoulders:0.20, FrontAbs:0.10`

### Shoulders

Overhead Press
- `FrontShoulders:1.00, BackShoulders:0.40, FrontTriceps:0.45, BackTriceps:0.20`

Dumbbell Shoulder Press
- `FrontShoulders:1.00, BackShoulders:0.40, FrontTriceps:0.45, BackTriceps:0.20`

Seated Dumbbell Shoulder Press
- `FrontShoulders:1.00, BackShoulders:0.40, FrontTriceps:0.45, BackTriceps:0.20`

Half-Kneeling Shoulder Press
- `FrontShoulders:1.00, BackShoulders:0.35, FrontTriceps:0.40, FrontAbs:0.15`

Machine Shoulder Press
- `FrontShoulders:1.00, BackShoulders:0.35, FrontTriceps:0.40, BackTriceps:0.15`

Arnold Press
- `FrontShoulders:1.00, BackShoulders:0.35, FrontTriceps:0.40`

Push Press
- `FrontShoulders:0.95, BackShoulders:0.35, FrontTriceps:0.35, FrontQuads:0.15, BackGlutes:0.10`

Landmine Press
- `FrontShoulders:0.80, FrontChest:0.30, FrontTriceps:0.30, BackShoulders:0.20, FrontAbs:0.15`

Lateral Raise
- `FrontShoulders:0.85, BackShoulders:0.35`

Cable Lateral Raise
- `FrontShoulders:0.85, BackShoulders:0.35`

Front Raise
- `FrontShoulders:1.00, FrontChest:0.10`

### Triceps

Triceps Dip
- `FrontTriceps:0.65, BackTriceps:0.80, FrontChest:0.25, FrontShoulders:0.20`

Dips
- `FrontTriceps:0.65, BackTriceps:0.80, FrontChest:0.25, FrontShoulders:0.20`

Skull Crushers
- `FrontTriceps:0.90, BackTriceps:1.00`

Overhead Triceps Extension
- `FrontTriceps:0.85, BackTriceps:1.00`

Cable Triceps Pushdown
- `FrontTriceps:0.85, BackTriceps:1.00`

Single-Arm Cable Triceps Extension
- `FrontTriceps:0.85, BackTriceps:1.00`

Dumbbell Kickback
- `BackTriceps:1.00, FrontTriceps:0.65, BackShoulders:0.15`

### Abs / Core

Crunches
- `FrontAbs:1.00, BackLowerBack:0.10`

Bicycle Crunches
- `FrontAbs:1.00, BackLowerBack:0.15`

Russian Twists
- `FrontAbs:0.90, BackLowerBack:0.20`

Cable Woodchopper
- `FrontAbs:0.95, BackLowerBack:0.20`

Ab Rollout
- `FrontAbs:1.00, BackLowerBack:0.30, FrontShoulders:0.10`

Flutter Kicks
- `FrontAbs:0.85, FrontQuads:0.10`

Hanging Leg Raise
- `FrontAbs:1.00, FrontBiceps:0.10, BackLats:0.10`

Hanging Knee Raise
- `FrontAbs:0.95, FrontBiceps:0.10, BackLats:0.10`

Plank
- `FrontAbs:1.00, BackLowerBack:0.30, FrontShoulders:0.10`

Plank Knee Drive
- `FrontAbs:1.00, BackLowerBack:0.25, FrontShoulders:0.10, FrontQuads:0.10`

Dead Bug
- `FrontAbs:0.90, BackLowerBack:0.35`

Bird Dog
- `FrontAbs:0.70, BackLowerBack:0.55, BackGlutes:0.20, BackShoulders:0.10`

Farmer Carry
- `FrontForearms:0.75, FrontAbs:0.75, BackTraps:0.50, BackLowerBack:0.35, FrontQuads:0.15, BackGlutes:0.15`

Suitcase Carry
- `FrontForearms:0.75, FrontAbs:0.85, BackTraps:0.40, BackLowerBack:0.40`

Pallof Press
- `FrontAbs:0.90, BackLowerBack:0.30, FrontShoulders:0.10`

### Legs

Squat
- `FrontQuads:1.00, BackGlutes:0.60, BackHamstrings:0.25, BackLowerBack:0.15`

Back Squat
- `FrontQuads:1.00, BackGlutes:0.65, BackHamstrings:0.25, BackLowerBack:0.20`

Pause Back Squat
- `FrontQuads:1.00, BackGlutes:0.65, BackHamstrings:0.25, BackLowerBack:0.20`

Front Squat
- `FrontQuads:1.00, BackGlutes:0.45, BackHamstrings:0.15, FrontAbs:0.15`

Goblet Squat
- `FrontQuads:0.95, BackGlutes:0.50, BackHamstrings:0.20, FrontAbs:0.10`

Box Squat
- `FrontQuads:0.90, BackGlutes:0.65, BackHamstrings:0.30, BackLowerBack:0.15`

Hack Squat
- `FrontQuads:1.00, BackGlutes:0.45, BackHamstrings:0.15`

Leg Press
- `FrontQuads:1.00, BackGlutes:0.40, BackHamstrings:0.15`

Leg Extension
- `FrontQuads:1.00`

Lunges
- `FrontQuads:0.90, BackGlutes:0.55, BackHamstrings:0.25`

Reverse Lunge
- `FrontQuads:0.85, BackGlutes:0.60, BackHamstrings:0.25`

Backward Lunge
- `FrontQuads:0.85, BackGlutes:0.60, BackHamstrings:0.25`

Walking Lunge
- `FrontQuads:0.85, BackGlutes:0.55, BackHamstrings:0.25, BackCalves:0.10`

Bulgarian Split Squat
- `FrontQuads:0.90, BackGlutes:0.60, BackHamstrings:0.25`

Rear-Foot Elevated Split Squat
- `FrontQuads:0.90, BackGlutes:0.60, BackHamstrings:0.25`

Supported Split Squat
- `FrontQuads:0.80, BackGlutes:0.45, BackHamstrings:0.20`

Step-Up
- `FrontQuads:0.85, BackGlutes:0.55, BackHamstrings:0.20, BackCalves:0.10`

Bodyweight Squat
- `FrontQuads:0.80, BackGlutes:0.40, BackHamstrings:0.15, BackCalves:0.05`

Single-Leg Balance Hold
- `FrontQuads:0.25, BackGlutes:0.35, BackCalves:0.25, FrontAbs:0.15`

Tandem Stance Hold
- `BackCalves:0.25, BackGlutes:0.20, FrontAbs:0.15`

Heel-to-Toe Walk
- `BackCalves:0.30, FrontQuads:0.15, BackGlutes:0.10`

Calf Raise
- `BackCalves:1.00`

Leg Curl
- `BackHamstrings:1.00, BackGlutes:0.15`

Hamstring Curl
- `BackHamstrings:1.00, BackGlutes:0.15`

Hip Thrust
- `BackGlutes:1.00, BackHamstrings:0.25, BackLowerBack:0.10`

## What To Change In Code Next

Best long-term approach:
- replace broad pattern heuristics with a central data table
- map each exercise name to its weighted regions
- fall back to movement-family rules only for unknown exercises

Recommended file:
- `WorkoutTracker.App/Resources/Raw/exercise_heat_map_weights.json`

Suggested shape:

```json
[
  {
    "Name": "Hammer Curl",
    "Regions": {
      "FrontForearms": 1.0,
      "FrontBiceps": 0.7
    }
  }
]
```

## Dumbbell Weight Entry Recommendation

Best UX for dumbbell exercises:
- let the user enter `weight per dumbbell`
- show a small badge or helper text like `x2 dumbbells`
- calculate total external load in the app as `entered weight * 2`

Why this is better:
- most lifters think of dumbbell work as "35s" or "50s", meaning each dumbbell
- it avoids confusion between "50 total" and "50 each"
- it lets one-arm dumbbell exercises stay `x1`

Recommended behavior:
- bilateral dumbbell exercises:
  - `Dumbbell Curl`
  - `Incline Dumbbell Curl`
  - `Incline Dumbbell Press`
  - `Dumbbell Fly`
  - `Dumbbell Shoulder Press`
  - `Seated Dumbbell Shoulder Press`
  - `Dumbbell Bench Press`
  - `Dumbbell Floor Press`
- default UI:
  - label: `Weight (each)`
  - helper: `x2 dumbbells`
- unilateral exercises:
  - keep `Weight`
  - helper: `x1 side`

Recommended fields if you want to store it properly:
- `WeightPerImplement`
- `ImplementCount`
- `TotalExternalLoad`

If you want the minimal version first:
- keep the existing `Weight` field
- relabel it to `Weight (each)` only for bilateral dumbbell exercises
- show `x2 dumbbells`
- multiply by 2 when saving

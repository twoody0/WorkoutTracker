namespace WorkoutTracker.Helpers;

public sealed record ExerciseInfo(string Summary, IReadOnlyList<string> Steps);

public static class ExerciseInfoCatalog
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Barbell Row"] = "Bent-Over Row",
        ["Chest Dip"] = "Dip",
        ["Cable Chest Fly"] = "Cable Crossover",
        ["Chest Press Machine"] = "Machine Chest Press",
        ["Dips"] = "Dip",
        ["Assisted Pull-Up"] = "Pull-Up",
        ["Elevated Push-Up"] = "Incline Push-Up",
        ["Face Pulls"] = "Face Pull",
        ["Rear-Foot Elevated Split Squat"] = "Bulgarian Split Squat",
        ["Weighted Pull-Up"] = "Pull-Up"
    };

    private static readonly Dictionary<string, ExerciseInfo> ExactInfoMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ab Rollout"] = Create("Builds the abs and teaches you to resist arching through the lower back.", "Start tall on the knees with the wheel under your shoulders.", "Roll forward while keeping the ribs down and hips tucked slightly.", "Stop when your trunk starts to lose position, then pull back to the start."),
        ["Arnold Press"] = Create("Works the shoulders through a long range of motion with help from the triceps.", "Start with the dumbbells in front of your shoulders and palms facing you.", "Rotate the palms out as you press overhead.", "Lower the dumbbells under control and rotate back to the start."),
        ["Band Pull-Apart"] = Create("Builds the rear shoulders and upper back with a simple band movement.", "Stand tall and hold the band at about shoulder height with soft elbows.", "Pull the hands apart by opening through the upper back instead of shrugging.", "Return slowly until the band loses tension without letting the ribs flare."),
        ["Back Squat"] = Create("Builds the quads, glutes, and trunk strength.", "Set the bar across the upper back, brace, and plant the feet firmly.", "Sit down and slightly back while keeping the whole foot on the floor.", "Drive up through the midfoot and stand tall without letting the chest collapse."),
        ["Barbell Bench Press"] = Create("Works the chest, front shoulders, and triceps.", "Lie on the bench with eyes under the bar and feet planted.", "Lower the bar to the lower chest with the wrists stacked over the forearms.", "Press the bar straight up until the elbows are straight but not slammed."),
        ["Barbell Curl"] = Create("Directly trains the biceps and forearms.", "Stand tall with the bar at arm's length and elbows close to your sides.", "Curl the bar without leaning back or swinging the hips.", "Lower the bar slowly until the arms are straight again."),
        ["Bent-Over Row"] = Create("Targets the lats, upper back, and biceps while challenging your hinge position.", "Hinge at the hips and brace until your torso is steady.", "Row the weight toward the lower ribs or upper stomach.", "Lower it back down without changing your torso angle."),
        ["Bird Dog"] = Create("Builds trunk control and teaches you to move the limbs without losing spinal position.", "Start on hands and knees with the spine neutral.", "Reach one arm and the opposite leg long while keeping the hips level.", "Return slowly and switch sides without rocking."),
        ["Bodyweight Good Morning"] = Create("Builds the hamstrings and glutes while teaching a clean hip hinge.", "Stand tall with the feet around hip width and brace lightly.", "Push the hips back with a soft knee bend while keeping the spine long.", "Stand back up by driving the hips forward instead of rounding up."),
        ["Bodyweight Squat"] = Create("Builds basic leg strength and reinforces the squat pattern.", "Stand with the feet around shoulder width and brace lightly.", "Sit down between the knees while keeping the heels on the floor.", "Stand back up by pushing the floor away."),
        ["Box Squat"] = Create("Teaches controlled squat depth and a strong drive out of the bottom.", "Set up the box so you can reach it without losing posture.", "Lower under control until you touch the box lightly.", "Stay braced and drive back up without rocking."),
        ["Bulgarian Split Squat"] = Create("Builds the quads and glutes one side at a time.", "Set the back foot on the bench and keep most of your weight on the front leg.", "Lower straight down until the front leg is well loaded.", "Push through the front foot to stand back up."),
        ["Cable Crossover"] = Create("Isolates the chest through a long, controlled range.", "Set the handles just below shoulder height and keep a soft bend in the elbows.", "Bring the hands together by squeezing through the chest.", "Open back up slowly until you feel a controlled stretch."),
        ["Cable Curl"] = Create("Trains the biceps with constant cable tension.", "Stand tall with the elbows close to the ribs.", "Curl the handle up without letting the shoulders roll forward.", "Lower slowly until the elbows are straight again."),
        ["Cable Lateral Raise"] = Create("Targets the side delts with smooth, even tension.", "Stand next to the pulley with the working arm slightly in front of the body.", "Lift the arm out to the side without shrugging.", "Lower with control until the shoulder is relaxed again."),
        ["Cable Triceps Pushdown"] = Create("Builds the triceps through elbow extension.", "Set the elbows by your sides before starting the rep.", "Press the handle down by straightening the elbows.", "Let the handle rise back up under control without the elbows drifting."),
        ["Cable Woodchopper"] = Create("Trains the abs and obliques through controlled rotation.", "Set the handle high or low and brace before you move.", "Rotate through the trunk while the hips stay mostly still.", "Return slowly to the start instead of letting the cable pull you back."),
        ["Calf Raise"] = Create("Targets the calves through a full ankle range of motion.", "Set the balls of the feet on the platform or floor and stay tall.", "Rise straight up onto the toes.", "Lower slowly into a comfortable stretch."),
        ["Dip"] = Create("Builds the chest, triceps, and front shoulders with your bodyweight on parallel bars or sturdy handles.", "Support yourself on the bars with the shoulders set down and the torso braced.", "Lower under control until the shoulders stay in a strong, comfortable position.", "Press back up smoothly without bouncing out of the bottom or losing tension."),
        ["Chest-Supported Row"] = Create("Builds the upper back and lats without asking the lower back to hold the hinge.", "Set the chest firmly on the pad before you pull.", "Row the handles by driving the elbows back.", "Lower under control and keep the chest on the pad."),
        ["Close-Grip Bench Press"] = Create("Builds the triceps while still training the chest and shoulders.", "Set up like a normal bench press with a slightly narrower grip.", "Lower the bar under control to the lower chest.", "Press back up while keeping the elbows tucked and wrists stacked."),
        ["Concentration Curl"] = Create("Trains the biceps with minimal momentum.", "Brace the elbow against the inner thigh or keep it fixed in place.", "Curl the weight up without moving the shoulder.", "Lower slowly until the arm is straight again."),
        ["Crunches"] = Create("Trains the abs with a short range spinal curl.", "Lie on the floor with knees bent and feet planted.", "Lift the shoulders by curling the ribcage toward the pelvis.", "Lower back down without yanking on the neck."),
        ["Dead Bug"] = Create("Builds core stability while the arms and legs move.", "Lie on your back with the knees and arms up.", "Reach the opposite arm and leg away while keeping the lower back quiet.", "Return to the start and switch sides slowly."),
        ["Deadlift"] = Create("Builds the glutes, hamstrings, back, and grip.", "Set the feet under the bar, brace hard, and pull the slack out.", "Push the floor away while keeping the bar close to your legs.", "Stand tall, then lower the bar by hinging and guiding it back down."),
        ["Decline Bench Press"] = Create("Targets the chest and triceps with a lower pressing angle.", "Set yourself tightly on the bench before you unrack.", "Lower the bar to a consistent touch point on the lower chest.", "Press straight up with control."),
        ["Diamond Push-Ups"] = Create("Emphasizes the triceps while still training the chest and shoulders.", "Set the hands close together under the chest.", "Lower as one piece with the elbows staying controlled.", "Press back up without letting the hips sag."),
        ["Dumbbell Bench Press"] = Create("Builds the chest, front shoulders, and triceps while letting each arm move freely.", "Set the shoulder blades and plant the feet before the first rep.", "Lower both dumbbells evenly to the sides of the chest.", "Press them back up without letting one side race ahead."),
        ["Dumbbell Curl"] = Create("Directly trains the biceps and forearms.", "Stand tall with the elbows close to the body.", "Curl the dumbbells up without swinging.", "Lower under control until the arms are straight."),
        ["Dumbbell Floor Press"] = Create("Trains the chest and triceps with a shorter, shoulder-friendly range.", "Lie on the floor and start with the dumbbells over the chest.", "Lower until the upper arms touch the floor lightly.", "Press back up without bouncing off the floor."),
        ["Dumbbell Fly"] = Create("Targets the chest with a controlled stretch and squeeze.", "Start with the dumbbells over the chest and elbows softly bent.", "Open the arms wide until the chest feels a stretch and the shoulders stay comfortable.", "Bring the weights back together by squeezing through the chest."),
        ["Dumbbell Kickback"] = Create("Targets the triceps when the upper arm stays fixed.", "Hinge forward and set the upper arm in line with the torso.", "Straighten the elbow until the arm is fully extended.", "Lower back to the start without swinging the shoulder."),
        ["Dumbbell Romanian Deadlift"] = Create("Builds the hamstrings and glutes through a controlled hinge.", "Hold the dumbbells close to the legs and soften the knees slightly.", "Push the hips back while keeping the spine long.", "Stand tall by driving the hips forward."),
        ["Dumbbell Shoulder Press"] = Create("Builds the shoulders and triceps with a free-weight press.", "Start with the dumbbells at shoulder height and ribs stacked.", "Press overhead without leaning back.", "Lower with control to the same shoulder position."),
        ["EZ-Bar Curl"] = Create("Trains the biceps with a grip that is often easier on the wrists.", "Stand tall and let the bar hang at arm's length.", "Curl the bar up while keeping the elbows still.", "Lower it slowly until the arms are straight."),
        ["Face Pull"] = Create("Builds the rear delts, upper back, and shoulder control.", "Set the rope around face height and grip it with thumbs pointing back.", "Pull toward the face while spreading the hands and lifting the elbows.", "Return slowly without letting the shoulders shrug."),
        ["Farmer Carry"] = Create("Builds grip, traps, and trunk stability.", "Stand tall with a weight in each hand and shoulders set down.", "Walk with short, controlled steps and a steady torso.", "Keep the ribs stacked over the hips until the set is done."),
        ["Flutter Kicks"] = Create("Builds lower-ab endurance and trunk control.", "Lie on your back and brace the abs before lifting the legs.", "Use small alternating kicks while keeping the lower back controlled.", "Keep the motion steady instead of turning it into a big swing."),
        ["Front Raise"] = Create("Targets the front delts.", "Stand tall with the ribs down and the weights by your thighs.", "Lift the arms forward with control until shoulder height.", "Lower slowly without swinging."),
        ["Front Squat"] = Create("Builds the quads, glutes, and upper-back posture.", "Set the bar on the front rack and lift the elbows high.", "Sit straight down while keeping the torso tall.", "Drive up through the midfoot and keep the elbows up."),
        ["Goblet Squat"] = Create("Builds the legs while teaching a strong squat pattern.", "Hold the weight close to the chest and brace.", "Lower between the knees while keeping the heels down.", "Stand back up tall without tipping forward."),
        ["Glute Bridge"] = Create("Builds the glutes and hamstrings with a simple floor-based hip extension.", "Lie on your back with the knees bent and feet flat on the floor.", "Brace lightly and drive through the heels until the hips are fully extended.", "Lower back down under control without arching the lower back."),
        ["Hack Squat"] = Create("Loads the quads heavily with machine support.", "Set the feet where you can stay balanced on the platform.", "Lower the sled under control until your depth stays strong.", "Press the sled away without snapping the knees locked."),
        ["Half-Kneeling Shoulder Press"] = Create("Builds the shoulders while adding a balance and core challenge.", "Set one knee down, squeeze the glute on that side, and brace.", "Press the weight straight overhead without leaning away.", "Lower back to shoulder height with control."),
        ["Hammer Curl"] = Create("Builds the biceps and brachialis with a neutral grip.", "Hold the dumbbells with palms facing each other.", "Curl them up without letting the torso rock.", "Lower slowly until the arms are straight."),
        ["Hamstring Curl"] = Create("Directly targets the hamstrings.", "Set the machine so the pad and pivot line up correctly.", "Curl the pad by pulling through the back of the legs.", "Lower slowly without letting the weight crash down."),
        ["Hanging Knee Raise"] = Create("Builds the abs while you control the hanging position.", "Hang tall from the bar and brace before moving.", "Lift the knees up without swinging the torso.", "Lower slowly to a dead hang."),
        ["Hanging Leg Raise"] = Create("Targets the abs through a longer hanging range than the knee raise.", "Hang from the bar and set the shoulders down.", "Raise the legs with control while keeping the swing small.", "Lower slowly until the body is steady again."),
        ["Heel-to-Toe Walk"] = Create("Builds balance and foot control.", "Stand tall and place one foot directly in front of the other.", "Walk in a straight line with small, careful steps.", "Keep the posture tall and eyes forward."),
        ["Hip Hinge Drill"] = Create("Teaches the hinge pattern used in deadlift and Romanian deadlift variations.", "Stand tall with soft knees and brace lightly.", "Push the hips back while keeping the spine long.", "Return to standing by driving the hips forward."),
        ["Hip Thrust"] = Create("Builds the glutes with strong hip extension.", "Set the upper back on the bench and the feet flat on the floor.", "Drive through the heels until the hips are fully extended.", "Lower back down without over-arching the lower back."),
        ["Incline Bench Press"] = Create("Targets the chest with extra upper-chest and front-shoulder emphasis.", "Set the upper back tight against the incline bench.", "Lower the bar to the upper chest under control.", "Press back up without shrugging the shoulders."),
        ["Incline Dumbbell Curl"] = Create("Trains the biceps from a stretched position.", "Sit back on the incline bench and let the arms hang behind the torso.", "Curl without letting the shoulders roll forward.", "Lower slowly to a full stretch."),
        ["Incline Dumbbell Press"] = Create("Builds the chest and shoulders with more upper-chest emphasis than a flat press.", "Set the shoulder blades on the incline bench and plant the feet.", "Lower the dumbbells evenly to the upper chest line.", "Press them up smoothly until the elbows are straight."),
        ["Incline Push-Up"] = Create("A beginner-friendly pressing pattern for the chest, shoulders, and triceps.", "Place the hands on the bench, box, or bar and make a straight line from shoulders to heels.", "Lower the chest toward the support under control.", "Press back up without letting the hips sag."),
        ["Landmine Press"] = Create("Builds the shoulders and upper chest through an angled press.", "Start with the bar at shoulder level and ribs stacked.", "Press up and slightly forward along the bar path.", "Lower back to the shoulder smoothly."),
        ["Lat Pulldown"] = Create("Targets the lats, upper back, and biceps.", "Set the chest tall and grip the bar just outside shoulder width.", "Pull the bar down by driving the elbows toward your sides.", "Let it rise back up under control to a full stretch."),
        ["Lateral Raise"] = Create("Targets the side delts.", "Stand tall with a slight bend in the elbows.", "Raise the arms out to the sides without shrugging.", "Lower slowly until the shoulders relax."),
        ["Leg Extension"] = Create("Directly targets the quads.", "Set the machine so the knee lines up with the pivot point.", "Extend the legs until the quads are contracted.", "Lower under control without dropping the weight."),
        ["Leg Press"] = Create("Builds the quads and glutes with machine support.", "Set the feet firmly on the platform and brace.", "Lower the sled under control while keeping the lower back supported.", "Press the platform away without bouncing at the bottom."),
        ["Machine Chest Press"] = Create("Builds the chest, front shoulders, and triceps with a guided path.", "Set the seat so the handles line up around chest height.", "Press the handles forward without shrugging the shoulders.", "Return with control until the chest is stretched again."),
        ["Machine Incline Press"] = Create("Targets the upper chest and shoulders with a guided press.", "Adjust the seat so the handles start near upper-chest height.", "Press smoothly until the elbows are straight.", "Lower back under control to the start."),
        ["Machine Shoulder Press"] = Create("Builds the shoulders and triceps with machine support.", "Set the seat so the handles start around shoulder height.", "Press overhead without arching the lower back.", "Lower under control to the start position."),
        ["Neutral-Grip Lat Pulldown"] = Create("Targets the lats and upper back with a shoulder-friendly grip.", "Set the chest tall and grab the neutral handles.", "Pull the handles down by driving the elbows toward the ribs.", "Let the weight rise back up slowly."),
        ["Overhead Press"] = Create("Builds the shoulders, triceps, and trunk.", "Start with the weight at shoulder height and the ribs stacked.", "Press straight overhead while keeping the torso still.", "Lower back to shoulder height under control."),
        ["Overhead Triceps Extension"] = Create("Targets the triceps, especially the long head.", "Hold the weight overhead with the upper arms mostly still.", "Lower behind the head by bending only at the elbows.", "Extend back up without flaring the ribs."),
        ["Pallof Press"] = Create("Builds anti-rotation core strength.", "Stand sideways to the cable or band and brace before moving.", "Press the handle straight out from the chest without turning.", "Bring it back in slowly while keeping the hips and shoulders square."),
        ["Pause Back Squat"] = Create("Builds squat control and strength out of the bottom position.", "Descend into your normal back squat with good tension.", "Pause briefly at the bottom without relaxing.", "Drive up while keeping the brace and bar path steady."),
        ["Pec Deck Fly"] = Create("Isolates the chest through a guided fly pattern.", "Set the seat so the handles line up with the chest.", "Bring the arms together by squeezing the chest.", "Open back up slowly until you feel a controlled stretch."),
        ["Pike Push-Up"] = Create("Builds the shoulders and triceps with a bodyweight pressing angle.", "Set up with the hands on the floor and hips lifted so the torso angles down toward the hands.", "Lower the head and shoulders toward the floor by bending the elbows under control.", "Press back up while keeping the hips lifted and the trunk braced."),
        ["Plank"] = Create("Builds the abs, glutes, and shoulder stability.", "Set the elbows or hands under the shoulders and straighten the body.", "Brace the abs and squeeze the glutes so the trunk stays flat.", "Hold that straight line until the set ends."),
        ["Plank Knee Drive"] = Create("Builds the abs while the hips move under a stable torso.", "Set a strong plank position with the shoulders stacked over the hands.", "Drive one knee in without rounding or dropping the trunk.", "Return to plank and switch sides under control."),
        ["Preacher Curl"] = Create("Directly targets the biceps with less chance to cheat.", "Set the upper arm against the pad before you start.", "Curl the weight up without lifting the elbow off the pad.", "Lower slowly until the arm is nearly straight."),
        ["Pull-Up"] = Create("Builds the lats, upper back, and biceps.", "Hang from the bar with a full grip and a tight torso.", "Pull by driving the elbows down and bringing the chest up.", "Lower to a full hang without dropping."),
        ["Push Press"] = Create("Builds shoulder strength and power with help from a short leg drive.", "Start with the weight at shoulder height and brace.", "Dip slightly, then drive through the legs and press overhead.", "Lower back to the shoulders under control."),
        ["Push-Up"] = Create("Builds the chest, shoulders, triceps, and trunk.", "Set the hands under the shoulders and make a straight line from head to heels.", "Lower the whole body together until the chest is close to the floor.", "Press back up without letting the hips sag or the shoulders shrug."),
        ["Rear Delt Cable Fly"] = Create("Targets the rear delts and upper back with cable tension.", "Set the cables so the arms can move across the body to start.", "Open the arms by squeezing through the back of the shoulders.", "Return slowly without shrugging."),
        ["Rear Delt Fly"] = Create("Targets the rear delts and upper back.", "Hinge slightly or set up on a bench so the torso is stable.", "Open the arms out to the sides with soft elbows.", "Lower slowly without swinging."),
        ["Resistance Band Row"] = Create("Builds the upper back and lats with a simple horizontal pull.", "Set the band and stand or sit tall before the first rep.", "Row by pulling the elbows back toward your ribs.", "Reach forward slowly so the shoulders stay under control."),
        ["Reverse Curl"] = Create("Targets the forearms and brachialis with a palms-down grip.", "Hold the bar with palms facing the floor and elbows close.", "Curl up without swinging the torso.", "Lower slowly while keeping the wrists firm."),
        ["Reverse Lunge"] = Create("Builds the quads and glutes one side at a time.", "Stand tall and step one leg back under control.", "Lower until both knees are bent and the front foot stays planted.", "Drive through the front foot to return to standing."),
        ["Reverse Pec Deck Fly"] = Create("Targets the rear delts and upper back on the machine.", "Set the seat so the handles line up with the shoulders.", "Open the arms by driving through the rear delts.", "Return slowly without letting the shoulders roll forward."),
        ["Romanian Deadlift"] = Create("Builds the hamstrings and glutes through a controlled hip hinge.", "Stand tall with the weight close to the body and knees softly bent.", "Push the hips back while keeping the spine long.", "Stand back up by driving the hips forward."),
        ["Russian Twists"] = Create("Builds the abs and obliques through controlled rotation.", "Sit tall and brace before you start rotating.", "Turn the ribcage from side to side instead of only moving the arms.", "Keep the motion smooth and controlled."),
        ["Seated Cable Row"] = Create("Targets the lats, upper back, and biceps.", "Sit tall with the chest up before you begin.", "Pull the handle toward the lower ribs by driving the elbows back.", "Reach forward under control without rounding hard."),
        ["Seated Calf Raise"] = Create("Targets the calves with the knees bent.", "Set the pad on the thighs and the balls of the feet on the platform.", "Rise up onto the toes as high as you can.", "Lower slowly into a stretch."),
        ["Seated Dumbbell Shoulder Press"] = Create("Builds the shoulders and triceps with the torso supported.", "Sit tall with the dumbbells at shoulder height.", "Press overhead without leaning back.", "Lower back to shoulder height under control."),
        ["Single-Arm Cable Triceps Extension"] = Create("Targets one triceps at a time.", "Set the elbow near your side or slightly in front of the body.", "Straighten the arm without moving the shoulder much.", "Return slowly to the start."),
        ["Single-Arm Dumbbell Row"] = Create("Builds the lats and upper back one side at a time.", "Support yourself on the bench or rack and keep the torso steady.", "Row the dumbbell toward the hip or lower ribs.", "Lower it slowly until the arm is straight again."),
        ["Single-Leg Balance Hold"] = Create("Builds balance and hip control.", "Stand tall on one leg with a soft knee bend.", "Hold the pelvis level and keep the foot active on the floor.", "Make small corrections without letting the torso sway."),
        ["Sit-to-Stand"] = Create("Builds leg strength and confidence getting up from a chair.", "Sit on the edge of the chair with the feet planted.", "Lean slightly forward and push through the feet to stand.", "Sit back down under control without dropping into the chair."),
        ["Skull Crushers"] = Create("Targets the triceps through elbow extension.", "Lie down with the weight above the shoulders and upper arms still.", "Lower the weight by bending at the elbows only.", "Extend back up smoothly to the start."),
        ["Step-Up"] = Create("Builds the quads, glutes, and balance one side at a time.", "Place the whole working foot on the step.", "Drive through that foot to stand tall on top.", "Step back down under control rather than dropping."),
        ["Standing Calf Raise"] = Create("Targets the calves with a standing position.", "Stand tall with the balls of the feet on the platform or floor.", "Rise straight up onto the toes.", "Lower slowly into a stretch without bouncing."),
        ["Stiff-Leg Deadlift"] = Create("Builds the hamstrings and glutes with less knee bend than a Romanian deadlift.", "Start tall with a slight bend in the knees and keep it mostly fixed.", "Hinge at the hips while the weight stays close.", "Stand back up by driving the hips through."),
        ["Suitcase Carry"] = Create("Builds grip and anti-side-bend core strength.", "Stand tall with one weight in one hand.", "Walk with even, controlled steps without leaning toward the load.", "Keep the ribs stacked over the hips the whole set."),
        ["Supported Split Squat"] = Create("Builds the legs with extra balance help.", "Set up in a split stance and lightly hold support.", "Lower straight down while keeping most of the load on the front leg.", "Drive through the front foot to stand back up."),
        ["T-Bar Row"] = Create("Builds the lats, rhomboids, and mid-back.", "Set the hinge and brace before you pull.", "Row the handle toward the lower chest or upper stomach.", "Lower with control instead of dropping the weight."),
        ["Tandem Stance Hold"] = Create("Builds balance by narrowing your base of support.", "Stand with one foot directly in front of the other.", "Brace lightly and keep the posture tall.", "Hold steady and breathe without shifting around."),
        ["Trap Bar Deadlift"] = Create("Builds the glutes, hamstrings, quads, and upper back.", "Stand inside the bar, brace, and pull the slack out of the handles.", "Push through the floor and stand tall while keeping the torso tight.", "Lower with control by hinging back down."),
        ["Walking Lunge"] = Create("Builds the quads, glutes, and balance through repeated steps.", "Step forward with control and plant the foot fully.", "Lower until both knees bend comfortably and the torso stays tall.", "Push through the front foot and move smoothly into the next step."),
        ["Wall Push-Up"] = Create("A very beginner-friendly press for the chest, shoulders, and triceps.", "Place the hands on the wall at about chest height and step back.", "Lower the chest toward the wall as one piece.", "Press away until the arms are straight.")
    };

    public static bool HasInfo(string? exerciseName)
        => TryGetInfo(exerciseName, out _);

    public static ExerciseInfo GetInfo(string? exerciseName)
        => TryGetInfo(exerciseName, out var info)
            ? info
            : Create(
                "A strength or movement exercise used in your workout plan.",
                "Set up in a stable position before the first rep.",
                "Move through a pain-free range with control.",
                "Finish the rep cleanly and reset before the next one.");

    public static bool TryGetInfo(string? exerciseName, out ExerciseInfo info)
    {
        if (string.IsNullOrWhiteSpace(exerciseName))
        {
            info = default!;
            return false;
        }

        var normalizedName = NormalizeExerciseName(exerciseName);
        return ExactInfoMap.TryGetValue(normalizedName, out info!);
    }

    private static string NormalizeExerciseName(string exerciseName)
    {
        var trimmed = exerciseName.Trim();
        return Aliases.TryGetValue(trimmed, out var canonicalName)
            ? canonicalName
            : trimmed;
    }

    private static ExerciseInfo Create(string summary, params string[] steps)
        => new(summary, steps);
}

using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Holt's hand-drawn spritesheet (Assets/Sprites/Generated/ProtagonistSpritesheet.png).
    /// Rows, top to bottom: idle gestures (6), walk (8), run (8), aim / fire / lower (8). When the
    /// sheet exists it replaces the placeholder Holt frames in AC_Holt and P_Player.
    /// Slicing and clip building live in Chapter1CharacterSheets.
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        private const string HoltSheetPath = "Assets/Sprites/Generated/ProtagonistSpritesheet.png";

        private static readonly CharacterSheet HoltSheet = new CharacterSheet
        {
            Path = HoltSheetPath,
            Prefix = "Holt",
            Ppu = 163f, // the walk / aim rows show Holt (1.85 m) about 301 px tall
            Rows = new[]
            {
                new SheetRow(0, "Idle", false), new SheetRow(1, "Walk", true),
                new SheetRow(2, "Run", true), new SheetRow(3, "Aim", false)
            }
        };

        // Holt's walk to the right (ProtagonistSpritesheetWalkRight.png): 5 rows of 6 frames, read
        // left to right, top to bottom. Holt is ~213 px tall there (301 px on the main sheet), so the
        // PPU keeps him the same size in the world.
        private static readonly CharacterSheet HoltWalkSheet = new CharacterSheet
        {
            Path = "Assets/Sprites/Generated/ProtagonistSpritesheetWalkRight.png",
            Prefix = "HoltWalk",
            Ppu = 114.6f,
            Rows = new[]
            {
                new SheetRow(0, "R0", true), new SheetRow(1, "R1", true), new SheetRow(2, "R2", true),
                new SheetRow(3, "R3", true), new SheetRow(4, "R4", true)
            }
        };

        /// <summary>The walk cycle: every frame of the walk sheet except the first three (standing starts).</summary>
        private static string[] HoltWalkFrames()
        {
            var frames = new System.Collections.Generic.List<string>();
            for (int row = 0; row < 5; row++)
            {
                for (int i = row == 0 ? 3 : 0; i < 6; i++)
                {
                    frames.Add($"R{row}_{i:00}");
                }
            }

            return frames.ToArray();
        }

        // Revolver muzzle in the aim frames, relative to the feet pivot (metres, facing right).
        private static readonly Vector3 HoltSheetMuzzle = new Vector3(0.91f, 1.45f, 0f);

        private static bool HasHoltSheet => HoltSheet.Exists;

        private static Sprite HoltFrame(string name) => SheetFrame(HoltSheet, name.Substring("Holt_".Length));

        /// <summary>The idle row is drawn ~11% larger than the rest of the sheet; scale it down to match.</summary>
        private static float HoltIdleScale() => SheetScale(HoltSheet, "Idle_00", "Aim_00");

        private static AnimationClip HoltSheetClip(string clipName, float fps, bool loop, float scale, params string[] frames) =>
            SheetClip(HoltSheet, clipName, fps, loop, scale, frames);

        /// <summary>AC_Holt from the sheet: idle / walk, interact and letter triggers, aim → fire → lower.</summary>
        private static RuntimeAnimatorController BuildHoltSheetController(string controllerPath)
        {
            float idleScale = HoltIdleScale();
            AssetDatabase.DeleteAsset(controllerPath);
            var holt = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            holt.AddParameter("Speed", AnimatorControllerParameterType.Float);
            holt.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
            holt.AddParameter("ReadLetter", AnimatorControllerParameterType.Trigger);
            holt.AddParameter("Aim", AnimatorControllerParameterType.Bool);
            holt.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = holt.layers[0].stateMachine;

            AnimatorState State(string name, Vector3 pos, AnimationClip clip)
            {
                AnimatorState st = sm.AddState(name, pos);
                st.motion = clip;
                return st;
            }

            var idle = State("Idle", new Vector3(250, 0), HoltSheetClip("Holt_Idle", 1, true, idleScale, "Idle_00"));
            // Walk: the walk sheet when it is there, else the 2nd, 3rd, 4th, 6th and 7th frames of the walk row.
            AnimationClip walkClip = HoltWalkSheet.Exists
                ? SheetClip(HoltWalkSheet, "Holt_Walk", 8, true, 1f, HoltWalkFrames())
                : HoltSheetClip("Holt_Walk", 7, true, 1f, "Walk_01", "Walk_02", "Walk_03", "Walk_05", "Walk_06");
            var walk = State("Walk", new Vector3(250, 100), walkClip);
            // Interact: hand to the chin; ReadLetter: hand at the coat — closest gestures on the sheet.
            var interact = State("Interact", new Vector3(500, 0), HoltSheetClip("Holt_Interact", 5, false, idleScale,
                "Idle_00", "Idle_03", "Idle_03", "Idle_00"));
            var read = State("ReadLetter", new Vector3(500, 100), HoltSheetClip("Holt_ReadLetter", 3, false, idleScale,
                "Idle_00", "Idle_04", "Idle_04", "Idle_04", "Idle_04", "Idle_00"));
            var raise = State("AimRaise", new Vector3(0, 250), HoltSheetClip("Holt_AimRaise", 14, false, 1f, "Aim_07", "Aim_06", "Aim_00"));
            var hold = State("AimHold", new Vector3(250, 250), HoltSheetClip("Holt_Aim", 1, true, 1f, "Aim_00"));
            var fire = State("Fire", new Vector3(500, 250), HoltSheetClip("Holt_Fire", 12, false, 1f, "Aim_04", "Aim_05", "Aim_00"));
            var lower = State("AimLower", new Vector3(250, 350), HoltSheetClip("Holt_AimLower", 12, false, 1f, "Aim_06", "Aim_07"));
            // Run frames are cut and ready for a future run; nothing plays them yet.
            HoltSheetClip("Holt_Run", 12, true, 1f, "Run_00", "Run_01", "Run_02", "Run_03", "Run_04", "Run_05", "Run_06", "Run_07");
            sm.defaultState = idle;

            Go(idle, walk, false).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            Go(walk, idle, false).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            foreach (var pair in new[] { (interact, "Interact"), (read, "ReadLetter") })
            {
                var t = sm.AddAnyStateTransition(pair.Item1);
                t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0f, pair.Item2);
                Go(pair.Item1, idle, true);
            }

            // Aim is entered from idle / walk only (not AnyState: the bool stays true while aiming).
            Go(idle, raise, false).AddCondition(AnimatorConditionMode.If, 0f, "Aim");
            Go(walk, raise, false).AddCondition(AnimatorConditionMode.If, 0f, "Aim");
            Go(raise, hold, true);
            Go(hold, fire, false).AddCondition(AnimatorConditionMode.If, 0f, "Fire");
            Go(fire, hold, true);
            foreach (var from in new[] { raise, hold, fire })
            {
                Go(from, lower, false).AddCondition(AnimatorConditionMode.IfNot, 0f, "Aim");
            }

            Go(lower, idle, true);
            return holt;
        }
    }
}

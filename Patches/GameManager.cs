using System;
using System.Collections;
using System.Linq;
using GlobalEnums;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;
using MonoMod.Cil;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable CS0414 // Remove unread private members

namespace Modding.Patches
{
    [MonoModPatch("global::GameManager")]
    public class GameManager : global::GameManager
    {
        [MonoModIgnore] private bool tilemapDirty;
        [MonoModIgnore] private bool waitForManualLevelStart;
        [MonoModIgnore] public new event DestroyPooledObjects DestroyPersonalPools;
        [MonoModIgnore] public new event UnloadLevel UnloadingLevel;
        [MonoModIgnore] public new event LevelReady NextLevelReady;
        [MonoModIgnore] public new Scene nextScene { get; private set; }

        [MonoModIgnore] private extern void ManualLevelStart();

        [MonoModReplace]
        public new IEnumerator LoadSceneAdditive(string destScene)
        {
            tilemapDirty = true;
            startedOnThisScene = false;
            nextSceneName = destScene;
            waitForManualLevelStart = true;
            DestroyPersonalPools?.Invoke();
            UnloadingLevel?.Invoke();
            string exitingScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            nextScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(destScene);

            // This is the only new line
            yield return Mod.OnBeforeAdditiveLoad(destScene);

            AsyncOperation loadop = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(destScene, LoadSceneMode.Additive);
            loadop.allowSceneActivation = true;
            yield return loadop;
            bool sceneUnloadOp = UnityEngine.SceneManagement.SceneManager.UnloadScene(exitingScene);
            RefreshTilemapInfo(destScene);
            ManualLevelStart();
            NextLevelReady?.Invoke();
        }

        [MonoModReplace]
        public new IEnumerator LoadFirstScene()
        {
            yield return new WaitForEndOfFrame();
            entryGateName = "top1";
            SetState(GameState.PLAYING);
            ui.ConfigureMenu();

            // This is the only new line
            Mod.OnNewGame();

            LoadScene(Constants.TUTORIAL_LEVEL);
        }

        [MonoModIgnore]
        [PatchLoad]
        public extern new bool LoadGame(int saveSlot);

        [MonoModIgnore]
        [PatchSave]
        public extern new void SaveGame(int saveSlot);

        // Custom attribute patching doesn't know about new classes
        internal static void OnAfterLoadGame(SaveGameData data)
            => Mod.OnAfterLoadGame(data);

        internal static void OnBeforeSaveGame(SaveGameData data)
            => Mod.OnBeforeSaveGame(data);
    }
}

namespace Modding
{
    [MonoModCustomAttribute(nameof(MonoModRules.PatchLoad))]
    internal class PatchLoadAttribute : Attribute { }

    [MonoModCustomAttribute(nameof(MonoModRules.PatchSave))]
    internal class PatchSaveAttribute : Attribute { }
}

namespace MonoMod
{
    static partial class MonoModRules
    {
        public static void PatchLoad(MethodDefinition method, CustomAttribute attrib)
        {
            ILCursor il = new ILCursor(new ILContext(method)).Goto(0);

            int loc = 0;
            il.GotoNext
            (
                MoveType.After,
                i => i.MatchCall(typeof(JsonUtility), nameof(JsonUtility.FromJson)),
                i => i.MatchStloc(out loc)
            );

            il.Emit(OpCodes.Ldloc_S, method.Body.Variables[loc]);
            il.Emit(OpCodes.Call, method.DeclaringType.Methods.First(m => m.Name == nameof(Modding.Patches.GameManager.OnAfterLoadGame)));
        }

        public static void PatchSave(MethodDefinition method, CustomAttribute attrib)
        {
            ILCursor il = new ILCursor(new ILContext(method)).Goto(0);

            int loc = 0;
            il.GotoNext
            (
                MoveType.After,
                i => i.MatchNewobj(nameof(SaveGameData)),
                i => i.MatchStloc(out loc)
            );

            il.Emit(OpCodes.Ldloc_S, method.Body.Variables[loc]);
            il.Emit(OpCodes.Call, method.DeclaringType.Methods.First(m => m.Name == nameof(Modding.Patches.GameManager.OnBeforeSaveGame)));
        }
    }
}

using MonoMod;

namespace Modding.Patches
{
    [MonoModPatch("global::SaveGameData")]
    public class SaveGameData : global::SaveGameData
    {
        public ModSettingsDictionary modSettings;

        [MonoModConstructor]
        public SaveGameData(PlayerData pd, SceneData sd) : base(pd, sd)
        {
            modSettings = new();
        }

        public void BeforeSave()
            => Mod.OnBeforeSaveGame(this);

        public void AfterLoad()
            => Mod.OnAfterLoadGame(this);
    }
}

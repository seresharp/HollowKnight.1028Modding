using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modding
{
    public abstract class Mod : Logger
    {
        internal static readonly List<Mod> ModList = new List<Mod>();

        public virtual string Version => "UNKNOWN";

        protected Mod(string name) : base(name)
        {
            Log("Initializing");
        }

        /// <summary>
        /// Gets a List of objects for the API to preload
        /// </summary>
        /// <returns>A <see cref="List{(scene name, object name, object id)}"/>, with id being an arbitrary given name for the object</returns>
        public virtual List<(string, string, string)> GetPreloads() => new();

        /// <summary>
        /// Sets a preloaded object, given by <see cref="GetPreloads"/>
        /// </summary>
        /// <param name="id">The id given by <see cref="GetPreloads"/></param>
        /// <param name="obj">The preloaded object (null if not found)</param>
        public virtual void SetPreload(string id, GameObject obj) { }

        public virtual ModSettings GetSaveSettings()
            => new ModSettings();

        public virtual void SetSaveSettings(ModSettings settings) { }

        public virtual void NewGame() { }

        public virtual float BeforeAdditiveLoad(string scene)
            => 0;

        internal static void OnBeforeSaveGame(Patches.SaveGameData data)
        {
            foreach (Mod mod in ModList)
            {
                try
                {
                    data.modSettings[mod.LogName] = mod.GetSaveSettings();
                }
                catch (Exception e)
                {
                    mod.Log($"Error in {nameof(GetSaveSettings)}\n{e}");
                }
            }
        }

        internal static void OnNewGame()
        {
            foreach (Mod mod in ModList)
            {
                try
                {
                    mod.SetSaveSettings(new());
                }
                catch (Exception e)
                {
                    mod.Log($"Error in {nameof(SetSaveSettings)}\n{e}");
                }

                try
                {
                    mod.NewGame();
                }
                catch (Exception e)
                {
                    mod.Log($"Error in {nameof(NewGame)}\n{e}");
                }
            }
        }

        internal static void OnAfterLoadGame(Patches.SaveGameData data)
        {
            foreach (Mod mod in ModList)
            {
                try
                {
                    if (!data.modSettings.TryGetValue(mod.LogName, out ModSettings settings))
                    {
                        settings = new();
                    }

                    mod.SetSaveSettings(settings);
                }
                catch (Exception e)
                {
                    mod.Log($"Error in {nameof(SetSaveSettings)}\n{e}");
                }
            }
        }

        internal static IEnumerator OnBeforeAdditiveLoad(string scene)
        {
            foreach (Mod mod in ModList)
            {
                float wait;
                try
                {
                    wait = mod.BeforeAdditiveLoad(scene);
                }
                catch (Exception e)
                {
                    mod.Log($"Error in {nameof(BeforeAdditiveLoad)}\n{e}");
                    continue;
                }

                yield return new WaitForSeconds(wait);
            }
        }
    }
}

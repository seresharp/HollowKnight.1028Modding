using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MonoMod;
using UnityEngine;
using UnityEngine.SceneManagement;

using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Modding.Patches
{
    [MonoModPatch("global::StartManager")]
    public class StartManager : global::StartManager
    {
        [MonoModReplace]
        private void Start()
        {
            LoadMods();
            StartCoroutine(Preload());

            ModVersionText.PopulateVersionList();
            DontDestroyOnLoad(new GameObject("Mod Version Text").AddComponent<ModVersionText>());
        }

        private void LoadMods()
        {
            Logger.API.Log("Initializing");

            string modsDir = Path.Combine(Application.dataPath, "Managed/Mods");
            if (!Directory.Exists(modsDir))
            {
                Directory.CreateDirectory(modsDir);
            }

            foreach (string modPath in Directory.GetFiles(modsDir, "*.dll"))
            {
                Logger.API.Log("Attempting to load mods from file " + Path.GetFileName(modPath));
                Type[] types;
                try
                {
                    types = Assembly.LoadFile(modPath).GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    Logger.API.Log("Failed loading some types from file");
                    foreach (Exception loaderException in e.LoaderExceptions)
                    {
                        Logger.API.Log(loaderException);
                    }

                    Logger.API.Log("Continuing with partial load");
                    types = e.Types.Where(t => t != null).ToArray();
                }
                catch (Exception e)
                {
                    Logger.API.Log("Failed loading types from file\n" + e);
                    continue;
                }

                foreach (Type t in types.Where(t => !t.IsGenericType && t.IsClass && t.IsSubclassOf(typeof(Mod))))
                {
                    Logger.API.Log("Attempting to load mod " + t.Name);

                    Mod mod;
                    try
                    {
                        mod = t.GetConstructor(new Type[0])?.Invoke(new object[0]) as Mod;
                    }
                    catch (Exception e)
                    {
                        Logger.API.Log("Load failed\n" + e);
                        continue;
                    }

                    if (mod == null)
                    {
                        Logger.API.Log("Load failed, mod type lacks parameterless constructor");
                        continue;
                    }

                    Mod.ModList.Add(mod);
                }
            }
        }

        private IEnumerator Preload()
        {
            // Scene, (Mod, object name, object id)
            Dictionary<string, List<(Mod, string, string)>> toPreload = new();
            foreach (Mod mod in Mod.ModList)
            {
                Logger.API.Log("Fetching preloads from mod " + mod.LogName);
                List<(string, string, string)> modPreloads;
                try
                {
                    modPreloads = mod.GetPreloads();
                }
                catch (Exception e)
                {
                    mod.Log("Failed getting preload names\n" + e);
                    continue;
                }

                foreach ((string scene, string obj, string id) in modPreloads)
                {
                    if (scene == null || obj == null || id == null)
                    {
                        Logger.API.Log($"Given null argument in preload ({scene ?? "null"}, {obj ?? "null"}, {id ?? "null"}), skipping");
                    }

                    if (!toPreload.TryGetValue(scene, out List<(Mod, string, string)> scenePreloads))
                    {
                        scenePreloads = new();
                        toPreload[scene] = scenePreloads;
                    }

                    scenePreloads.Add((mod, obj, id));
                }
            }

            progressIndicator.gameObject.SetActive(true);
            progressIndicator.minValue = 0;
            progressIndicator.maxValue = toPreload.Count + 1;

            int preloadIdx = 0;
            foreach (string sceneName in toPreload.Keys)
            {
                Logger.API.Log("Preloading objects in scene " + sceneName);

                AsyncOperation loadop = USceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                while (!loadop.isDone)
                {
                    progressIndicator.value = preloadIdx + loadop.progress / .9f;
                    yield return null;
                }

                Scene scene = USceneManager.GetSceneByName(sceneName);
                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach ((Mod mod, string objPath, string objId) in toPreload[sceneName])
                {
                    string rootName;
                    string childName;
                    if (objPath.Contains('/'))
                    {
                        int slash = objPath.IndexOf('/');
                        if (slash == 0 || slash == objPath.Length - 1)
                        {
                            Logger.API.Log($"Malformatted object path '{objPath}' given by mod {mod.LogName}, skipping");
                            continue;
                        }

                        rootName = objPath.Substring(0, slash);
                        childName = objPath.Substring(slash + 1);
                    }
                    else
                    {
                        rootName = objPath;
                        childName = null;
                    }

                    GameObject obj = rootObjects.FirstOrDefault(o => o.name == rootName);
                    if (childName != null && obj != null)
                    {
                        Transform transform = obj.transform.Find(childName);
                        if (transform != null)
                        {
                            obj = transform.gameObject;
                        }
                    }

                    if (obj == null)
                    {
                        Logger.API.Log($"Couldn't find object with path '{objPath}' requested by mod {mod.LogName}");
                        continue;
                    }

                    obj = Instantiate(obj);
                    DontDestroyOnLoad(obj);
                    obj.SetActive(false);

                    try
                    {
                        mod.SetPreload(objId, obj);
                    }
                    catch (Exception e)
                    {
                        mod.Log($"Failed setting preloaded object '{objId}'\n{e}");
                        continue;
                    }

                    Logger.API.Log($"Successfully preloaded object '{objId}' for mod {mod.LogName}");
                }

                USceneManager.UnloadScene(scene);
                preloadIdx++;
            }

            AsyncOperation menuLoadop = USceneManager.LoadSceneAsync(Constants.MENU_SCENE);
            while (!menuLoadop.isDone)
            {
                progressIndicator.value = preloadIdx + menuLoadop.progress / .9f;
                yield return null;
            }
        }
    }
}

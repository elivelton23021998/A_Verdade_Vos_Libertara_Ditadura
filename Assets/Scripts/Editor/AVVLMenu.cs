﻿using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;
using AVVL.JsonManager;
using UnityEngine;
using UnityEditor;

public class AVVLMenu : EditorWindow
{
    private bool encrypt;
    private FilePath filePath;
    private string key;

    const string f_scriptables = "Assets/AVVL_Package/AVVL Assets/Scriptables/";
    const string f_gameSettings = "Assets/AVVL_Package/AVVL Assets/Scriptables/Game Settings";

    [MenuItem("Tools/AVVL/Setup/Game/FirstPerson")]
    static void SetupFPS()
    {
        GameObject GameManager = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Setup/Game/GAMEMANAGER")) as GameObject;
        GameObject Player = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Setup/Game/FPSPLAYER")) as GameObject;

        Player.transform.position = new Vector3(0, 0, 0);
        GameManager.GetComponent<AVVL_GameManager>().Player = Player;
        GameManager.GetComponent<SaveGameHandler>().saveableDataPairs = new SaveableDataPair[0];
        Player.GetComponentInChildren<ScriptManager>().m_GameManager = GameManager.GetComponent<AVVL_GameManager>();
        Player.GetComponentInChildren<ScriptManager>().m_InputController = GameManager.GetComponent<InputController>();
    }

    [MenuItem("Tools/AVVL/Setup/Game/FirstPerson", true)]
    static bool CheckSetupFPS()
    {
        if (GameObject.Find("MENUMANAGER"))
        {
            return false;
        }

        if (GameObject.Find("GAMEMANAGER"))
        {
            return false;
        }

        if (GameObject.Find("FPSPLAYER"))
        {
            return false;
        }

        return true;
    }

    [MenuItem("Tools/AVVL/Setup/Game/FirstPerson Body")]
    static void SetupFPSB()
    {
        GameObject GameManager = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Setup/Game/GAMEMANAGER")) as GameObject;
        GameObject Player = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Setup/Game/HEROPLAYER")) as GameObject;

        Player.transform.position = new Vector3(0, 0, 0);
        GameManager.GetComponent<AVVL_GameManager>().Player = Player;
        GameManager.GetComponent<SaveGameHandler>().saveableDataPairs = new SaveableDataPair[0];
        Player.GetComponentInChildren<ScriptManager>().m_GameManager = GameManager.GetComponent<AVVL_GameManager>();
        Player.GetComponentInChildren<ScriptManager>().m_InputController = GameManager.GetComponent<InputController>();
    }

    [MenuItem("Tools/AVVL/Setup/Game/FirstPerson Body", true)]
    static bool CheckSetupFPSB()
    {
        if (GameObject.Find("MENUMANAGER"))
        {
            return false;
        }

        if (GameObject.Find("GAMEMANAGER"))
        {
            return false;
        }

        if (GameObject.Find("HEROPLAYER"))
        {
            return false;
        }

        return true;
    }

    [MenuItem("Tools/AVVL/Setup/MainMenu")]
    static void SetupMainMenu()
    {
        if (DestroyAll())
        {
            GameObject MenuManager = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Setup/MainMenu/MENUMANAGER")) as GameObject;
        }
    }

    [MenuItem("Tools/AVVL/Setup/MainMenu", true)]
    static bool CheckSetupMainMenu()
    {
        if (GameObject.Find("MENUMANAGER"))
        {
            return false;
        }

        if (GameObject.Find("GAMEMANAGER"))
        {
            return false;
        }

        if (GameObject.Find("FPSPLAYER"))
        {
            return false;
        }

        return true;
    }

    [MenuItem("Tools/AVVL/Setup/Fix Setup")]
    static void FixGameSetup()
    {
        if (DestroyAll())
        {
            GameObject GameManager;
            GameObject Player;

            if (GameObject.Find("GAMEMANAGER"))
            {
                GameManager = GameObject.Find("GAMEMANAGER");
            }
            else
            {
                GameManager = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Setup/Game/GAMEMANAGER")) as GameObject;
            }

            if (GameObject.Find("FPSPLAYER"))
            {
                Player = GameObject.Find("FPSPLAYER");
            }
            else
            {
                Player = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Setup/Game/FPSPLAYER")) as GameObject;
            }

            GameManager.GetComponent<AVVL_GameManager>().Player = Player;
            Player.GetComponentInChildren<ScriptManager>().m_GameManager = GameManager.GetComponent<AVVL_GameManager>();
            Player.GetComponentInChildren<ScriptManager>().m_InputController = GameManager.GetComponent<InputController>();

            EditorUtility.SetDirty(GameManager.GetComponent<AVVL_GameManager>());
            EditorUtility.SetDirty(Player.GetComponentInChildren<ScriptManager>());

            Debug.Log("<color=green>Tudo deve estar bem! </color>");
        }
    }

    static bool DestroyBase()
    {
        if (Camera.main.gameObject != null)
        {
            DestroyImmediate(Camera.main.gameObject);
            return true;
        }

        return true;
    }

    static bool DestroyAll()
    {
        if (FindObjectsOfType<GameObject>().Length > 0)
        {
            foreach (GameObject o in FindObjectsOfType<GameObject>().Select(obj => obj.transform.root.gameObject).ToArray())
            {
                DestroyImmediate(o);
            }

            if (FindObjectsOfType<GameObject>().Length < 1)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    [MenuItem("Tools/AVVL/" + "Scriptables" + "/New Inventory Database")]
    static void CreateInventoryDatabase()
    {
        CreateAssetFile<InventoryScriptable>("InventoryDatabase");
    }

    [MenuItem("Tools/AVVL/" + "Scriptables" + "/New Scene Objectives")]
    static void CreateObjectiveDatabase()
    {
        CreateAssetFile<ObjectivesScriptable>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " Objectives", "Objectives");
    }

    [MenuItem("Tools/AVVL/" + "Scriptables" + "/New Graphic Settings")]
    static void CreateOptionsAsset()
    {
        CreateAssetFile<GraphicScriptable>("GraphicSettings");
    }

    [MenuItem("Tools/AVVL/" + "Scriptables" + "/New Input Mapper")]
    static void CreateInputsAsset()
    {
        CreateAssetFile<InputScriptable>("InputMapper");
    }

    [MenuItem("Tools/AVVL/" + "Scriptables" + "/New Save\\Load Settings")]
    public static void ShowWindow()
    {
        GetWindow<AVVLMenu>(false, "Save/Load Editor", true);
    }

    [MenuItem("Tools/AVVL/Add FloatingIcons")]
    static void AddFloatingIcon()
    {
        if (Selection.gameObjects.Length > 0)
        {
            FloatingIconManager uIFloatingItem = FindObjectOfType<FloatingIconManager>();

            foreach (var obj in Selection.gameObjects)
            {
                uIFloatingItem.FloatingIcons.Add(obj);
            }

            EditorUtility.SetDirty(uIFloatingItem);
            Debug.Log("<color=green>" + Selection.gameObjects.Length + " objetos são marcados como um ícone flutuante</ color>");
        }
        else
        {
            Debug.Log("<color=red>Selecione um ou mais itens que serão marcados como Ícone Flutuante</color>");
        }
    }

    void OnGUI()
    {
        encrypt = EditorGUILayout.Toggle("Encrypt Data:", encrypt);
        filePath = (FilePath)EditorGUILayout.EnumPopup("File Path:", FilePath.GameDataPath);
        key = EditorGUILayout.TextField("Cipher Key", key);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Create", GUILayout.Width(100), GUILayout.Height(30)))
        {
            SaveLoadScriptable asset = CreateInstance<SaveLoadScriptable>();

            asset.enableEncryption = encrypt;
            asset.filePath = filePath;
            asset.cipherKey = MD5Hash(key);

            AssetDatabase.CreateAsset(asset, f_gameSettings + "SaveLoadSettings" + ".asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
    }

    private static void CreateAssetFile<T>(string AssetName, string Folder = "Game Settings") where T : ScriptableObject
    {
        var asset = CreateInstance<T>();

        if (!AssetDatabase.IsValidFolder(f_scriptables + Folder))
        {
            Debug.Log("Create Folder: " + f_scriptables + Folder);
            AssetDatabase.CreateFolder(f_scriptables, Folder);
        }

        ProjectWindowUtil.CreateAsset(asset, f_scriptables + Folder + "/New " + AssetName + ".asset");
    }

    public static string MD5Hash(string Data)
    {
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(Data));

        StringBuilder stringBuilder = new StringBuilder();

        foreach (byte b in hash)
        {
            stringBuilder.AppendFormat("{0:x2}", b);
        }

        return stringBuilder.ToString();
    }

    private static string GetPath()
    {
        if (Directory.Exists(f_gameSettings))
        {
            if (Directory.GetFiles(f_gameSettings).Length > 0)
            {
                return JsonManager.GetFilePath((AVVL.JsonManager.FilePath)AssetDatabase.LoadAssetAtPath<SaveLoadScriptable>(f_gameSettings + "SaveLoadSettings.asset").filePath);
            }
            return JsonManager.GetFilePath(AVVL.JsonManager.FilePath.GameDataPath);
        }
        else
        {
            return JsonManager.GetFilePath(AVVL.JsonManager.FilePath.GameDataPath);
        }
    }
}

public static class ScriptableFinder
{
    public static T GetScriptable<T>(string AssetName) where T : ScriptableObject
    {
        string path = "Assets/AVVL_Package/AVVL Assets/Scriptables/Game Settings/";

        if (Directory.Exists(path))
        {
            if (Directory.GetFiles(path).Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<T>(path + AssetName + ".asset");
            }
            return null;
        }
        else
        {
            return null;
        }
    }
}

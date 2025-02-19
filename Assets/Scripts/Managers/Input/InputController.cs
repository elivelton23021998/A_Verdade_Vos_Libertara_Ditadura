﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// Script que controla as principais funções de entrada
/// </summary>
public class InputController : Singleton<InputController> {

    private ConfigHandler configHandler;
    private UICustomOptions Options;

    [Header("Principal")]
    public InputScriptable inputMapper;
    public Transform inputsContent;
    public GameObject inputObject;

    private List<InputMap> InputsList = new List<InputMap>();

    private bool rebind;
	private Text buttonText;
	private string inputName;
	private string defaultKey;

    void Awake()
    {
        if (GetComponent<ConfigHandler>() && GetComponent<UICustomOptions>())
        {
            configHandler = GetComponent<ConfigHandler>();
            Options = GetComponent<UICustomOptions>();
        }
    }

    void Start () {
        if (!GetComponent<ConfigHandler>() && !GetComponent<UICustomOptions>())
        {
            Debug.LogError("Input Error: Script ConfigHandler ou UICustomOptions ausente em " + gameObject.name);
            return;
        }

        if (!inputMapper)
        {
            Debug.LogError("Input Error: O campo Mapeador de entrada não pode ser nulo!");
            return;
        }

        if (!inputsContent && !inputObject)
        {
            Debug.LogError("Input Error: Existem campos que não podem estar vazios!(inputsContent or inputObject)");
            return;
        }

        if (!configHandler.ContainsSection("Input"))
        {
            Debug.LogError("Input Error: Não há seção \"Input\" no arquivo de configuração!");
        }

        if (!configHandler.Error && configHandler.ContainsSection("Input"))
        {
            GenerateInputs(false);
        }
        else
        {
            GenerateInputs(true);
        }
    }

    private void GenerateInputs(bool useDefault)
    {
        if (configHandler.ContainsSection("Input"))
        {
            if (configHandler.GetKeysSectionCount("Input") > inputMapper.inputMap.Count)
            {
                configHandler.RemoveSection("Input");
            }
        }

        foreach (var iMap in inputMapper.inputMap)
        {
            GameObject InputObject = Instantiate(inputObject);
            Text InputText = InputObject.transform.GetChild(0).GetComponent<Text>();
            Button InputButton = InputObject.transform.GetChild(1).GetComponent<Button>();
            string InputName = iMap.InputName;
            KeyCode InputKey = iMap.DefaultKey;

            InputObject.transform.SetParent(inputsContent);
            InputObject.transform.localScale = new Vector3(1, 1, 1);
            InputObject.name = InputName;
            InputText.text = InputName.ToUpper();

            if (useDefault)
            {
                InputButton.transform.GetChild(0).GetComponent<Text>().text = InputKey.ToString();
                configHandler.Serialize("Input", InputName, InputKey.ToString());
            }
            else
            {
                KeyCode DeserializedKey = KeyCode.None;

                if (configHandler.ContainsSectionKey("Input", InputName))
                {
                    DeserializedKey = Parser.Convert<KeyCode>(configHandler.Deserialize("Input", InputName));
                }

                if (inputMapper.RewriteConfig)
                {
                    if (DeserializedKey != InputKey)
                    {
                        configHandler.Serialize("Input", InputName, InputKey.ToString());
                    }
                    else
                    {
                        InputKey = DeserializedKey;
                    }
                }
                else
                {
                    InputKey = DeserializedKey;
                }

                InputButton.transform.GetChild(0).GetComponent<Text>().text = InputKey.ToString();
            }

            InputButton.onClick.AddListener(delegate { Rebind(InputName); });
            InputsList.Add(new InputMap(InputName, InputKey, InputButton));
        }
    }
	
	public void Rebind(string InputName)
	{
        foreach(var input in InputsList)
        {
            if (InputName == input.Input && !rebind)
            {
                buttonText = input.InputButton.transform.GetChild(0).GetComponent<Text>();
                defaultKey = buttonText.text;
                buttonText.text = "Pressione a Tecla";
                inputName = input.Input;
                rebind = true;
            }

            input.InputButton.interactable = false;
        }

        Options.ApplyButton.interactable = false;
        Options.BackButton.interactable = false;
    }
	
	void Update()
	{
		foreach(KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
		{
			if (Input.GetKeyDown (kcode) && rebind) {
                if (kcode != KeyCode.Escape)
                {
                    if (kcode.ToString() == defaultKey)
                    {
                        buttonText.text = defaultKey;
                        buttonText = null;
                        inputName = null;
                        rebind = false;

                        Options.ApplyButton.interactable = true;
                        Options.BackButton.interactable = true;

                        foreach (var input in InputsList)
                        {
                            input.InputButton.interactable = true;
                        }
                    }
                    else
                    {
                        RebindKey(kcode.ToString());
                    }
                }
                else
                {
                    BackRewrite();
                }
			}
		}
	}

	void RebindKey(string kcode)
	{
		if (!ContainsKey(kcode)) {
			buttonText.text = kcode;
			SerializeInput (inputName, kcode);
            UpdateInputs();
			buttonText = null;
			inputName = null;
			rebind = false;

            Options.ApplyButton.interactable = true;
            Options.BackButton.interactable = true;

            foreach (var input in InputsList)
            {
                input.InputButton.interactable = true;
            }
        } else {
            Options.DuplicateInputGo.SetActive(true);
            Options.DuplicateInputGo.transform.GetChild(0).GetComponent<Text>().text = "Tecla \"" + kcode + "\" já está definida";
            Options.RewriteKeycode = kcode;
			rebind = false;
		}
	}

    public void Rewrite(string RewriteKeycode)
    {
        foreach (var input in InputsList)
        {
            Text DuplicateKeyText = input.InputButton.transform.GetChild(0).gameObject.GetComponent<Text>();

            if (DuplicateKeyText.text == RewriteKeycode)
            {
                DuplicateKeyText.text = "None";
                SerializeInput(input.Input, "None");
            }

            input.InputButton.interactable = true;
        }

        Options.ApplyButton.interactable = true;
        Options.BackButton.interactable = true;

        buttonText.text = RewriteKeycode;
        SerializeInput(inputName, RewriteKeycode);
        UpdateInputs();
        inputName = null;
    }

    public void BackRewrite()
    {
        buttonText.text = defaultKey;
        buttonText = null;
        inputName = null;
        rebind = false;

        Options.ApplyButton.interactable = true;
        Options.BackButton.interactable = true;

        foreach (var input in InputsList)
        {
            input.InputButton.interactable = true;
        }
    }

    public void RefreshInputs()
    {
        UpdateInputs();
    }
	
	void SerializeInput(string input, string button)
	{
        configHandler.Serialize("Input", input, button);
	}

    void UpdateInputs()
    {
        foreach (var input in InputsList)
        {
            string key = configHandler.Deserialize("Input", input.Input);

            if(input.Key.ToString() != key)
            {
                input.Key = Parser.Convert<KeyCode>(key);
            }
        }
    }

    /// <summary>
    /// Obter entrada como KeyCode por InputName
    /// </summary>
    public KeyCode GetInput(string InputName)
    {
        if (ContainsInput(InputName))
        {
            foreach (var input in InputsList)
            {
                if (input.Input == InputName)
                {
                    return input.Key;
                }
            }
        }

        return KeyCode.None;
    }

    /// <summary>
    /// Verifique se InputsList contém Input
    /// </summary>
    public bool ContainsInput(string inputName)
    {
        foreach (var input in InputsList)
        {
            if (input.Input == inputName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifique se InputsList contém uma Tecla
    /// </summary>
    public bool ContainsKey(string Key)
    {
        foreach (var input in InputsList)
        {
            if (input.Key.ToString() == Key)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Obter Contagem da InputsList
    /// </summary>
    public int Count()
    {
        return InputsList.Count;
    }

    /// <summary>
    /// Verifique se InputsList tem Inputs
    /// </summary>
    public bool HasInputs()
    {
        return InputsList.Count > 0;
    }
}

[Serializable]
public class InputMap
{
    public string Input;
    public KeyCode Key;
    public Button InputButton;

    public InputMap(string input, KeyCode key, Button btn)
    {
        Input = input;
        Key = key;
        InputButton = btn;
    }
}

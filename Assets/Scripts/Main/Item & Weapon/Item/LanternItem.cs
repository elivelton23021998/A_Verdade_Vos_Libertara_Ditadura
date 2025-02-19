﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanternItem : MonoBehaviour, ISwitcher, ISaveableArmsItem {

    private AVVL_GameManager gameManager;
    private ScriptManager scriptManager;
    private InputController inputControl;
    private ItemSwitcher switcher;
    private Inventory Inventory;
    private Animation anim;
    private AudioSource audioS;

    [Header("Principal")]
    public Light LanternLight;
    [Space(7)]
    public float oilLifeInSec = 300f;
    public float oilPercentage = 100;
    public float lightReductionRate = 5f;
    public float canReloadPercent;
    public float hideIntensitySpeed;
    public float oilReloadSpeed;
    public float timeWaitToReload;

    [Header("Inventário")]
    public int lanternInventoryID;
    private int switcherID;

    [Header("Animação")]
    public GameObject LanternGO;
    public string DrawAnim;
    [Range(0, 5)] public float DrawSpeed = 1f;
    public string HideAnim;
    [Range(0, 5)] public float HideSpeed = 1f;
    public string ReloadAnim;
    [Range(0, 5)] public float ReloadSpeed = 1f;
    public string IdleAnim;

    [Header("Sons")]
    public AudioClip ShowSound;
    [Range(0, 1)] public float ShowVolume;
    public AudioClip HideSound;
    [Range(0, 1)] public float HideVolume;
    public AudioClip ReloadOilSound;
    [Range(0, 1)] public float ReloadVolume;

    private KeyCode UseItemKey;

    private bool isSelected;
    private bool isSelecting;
    private bool isReloading;
    private bool isPressed;

    public float reductionFactor;
    public float reduceIntensity;
    public float oldIntensity;

    private float fullIntnesity;
    private float defaultOilPercentagle;
    private string currentSprite;

    private Color FlameTint;

    [HideInInspector]
    public bool CanReload;

    void Awake()
    {
        anim = LanternGO.GetComponent<Animation>();
        scriptManager = transform.root.GetComponentInChildren<ScriptManager>();
        switcher = transform.root.GetComponentInChildren<ItemSwitcher>();
        gameManager = AVVL_GameManager.Instance;

        if (LanternGO.GetComponent<AudioSource>())
        {
            audioS = LanternGO.GetComponent<AudioSource>();
        }

        defaultOilPercentagle = oilPercentage;
        fullIntnesity = LanternLight.intensity;
        oldIntensity = LanternLight.intensity;
        reduceIntensity = LanternLight.intensity;
        FlameTint = LanternLight.transform.GetChild(0).GetComponent<MeshRenderer>().material.GetColor("_Color");
        FlameTint.a = 0f;
        LanternLight.intensity = 0f;
        reductionFactor = oilPercentage - lightReductionRate;
    }

    void Start()
    {
        inputControl = scriptManager.GetScript<InputController>();
        Inventory = scriptManager.GetScript<Inventory>();
        switcherID = switcher.GetIDByObject(gameObject);

        anim[DrawAnim].speed = DrawSpeed;
        anim[HideAnim].speed = HideSpeed;
        anim[ReloadAnim].speed = ReloadSpeed;
    }

    public void Reload()
    {
        if (LanternGO.activeSelf)
        {
            if (oilPercentage < canReloadPercent && !isReloading)
            {
                StartCoroutine(ReloadCorountine());
                isReloading = true;
            }
        }
    }

    IEnumerator ReloadCorountine()
    {
        anim.Play(ReloadAnim);

        yield return new WaitForSeconds(timeWaitToReload);

        if (audioS && ReloadOilSound)
        {
            audioS.clip = ReloadOilSound;
            audioS.volume = ReloadVolume;
            audioS.Play();
        }

        while (LanternLight.intensity <= fullIntnesity)
        {
            LanternLight.intensity += Time.deltaTime * oilReloadSpeed;
            yield return null;
        }

        oilPercentage = defaultOilPercentagle;
        reductionFactor = oilPercentage - lightReductionRate;
        LanternLight.intensity = fullIntnesity;
        reduceIntensity = fullIntnesity;
        FlameTint.a = fullIntnesity;

        isReloading = false;
    }

    public void Select()
    {
        isSelecting = true;
        LanternGO.SetActive(true);
        LanternLight.gameObject.SetActive(true);

        anim.Play(DrawAnim);

        if (audioS && ShowSound)
        {
            audioS.clip = ShowSound;
            audioS.volume = ShowVolume;
            audioS.Play();
        }

        gameManager.ShowLightPercentagle(oilPercentage);
        StartCoroutine(SelectCoroutine());
    }

    IEnumerator SelectCoroutine()
    {
        while (LanternLight.intensity <= oldIntensity)
        {
            LanternLight.intensity += Time.deltaTime * hideIntensitySpeed;
            FlameTint.a += Time.deltaTime * hideIntensitySpeed;
            yield return null;
        }

        FlameTint.a = oldIntensity;
        LanternLight.intensity = oldIntensity;
        isSelected = true;
    }

    public void Deselect()
    {
        isSelecting = false;
        oldIntensity = LanternLight.intensity;

        if (audioS && HideSound)
        {
            audioS.clip = HideSound;
            audioS.volume = HideVolume;
            audioS.Play();
        }

        if (LanternGO.activeSelf)
        {
            gameManager.ShowLightPercentagle(oilPercentage, false);
            StartCoroutine(DeselectCoroutine());
        }
    }

    IEnumerator DeselectCoroutine()
    {
        anim.Play(HideAnim);

        while (LanternLight.intensity >= 0.01f)
        {
            LanternLight.intensity -= Time.deltaTime * hideIntensitySpeed;
            FlameTint.a -= Time.deltaTime * hideIntensitySpeed;
            yield return null;
        }

        LanternLight.intensity = 0f;

        yield return new WaitUntil(() => !anim.isPlaying);

        isSelected = false;
    }

    public void Disable()
    {
        LanternLight.intensity = 0f;
        gameManager.ShowLightPercentagle(oilPercentage, false);

        isSelecting = false;
        isSelected = false;
    }

    public void EnableItem()
    {
        Debug.Log("Enable");
        isSelected = true;
        LanternGO.SetActive(true);
        gameManager.ShowLightPercentagle(oilPercentage);
        anim.Play(IdleAnim);
    }

    void Update()
    {
        if (inputControl.HasInputs())
        {
            UseItemKey = inputControl.GetInput("Lanterna");
        }

        CanReload = oilPercentage < canReloadPercent;

        if (Inventory.CheckItemInventory(lanternInventoryID) && switcher.currentLightObject == switcherID)
        {
            if (Input.GetKeyDown(UseItemKey) && !anim.isPlaying && !isPressed)
            {
                if (!isSelected && switcher.currentItem != switcherID)
                {
                    switcher.SelectItem(switcherID);
                }
                else
                {
                    Deselect();
                }
                isPressed = true;
            }
            else if (isPressed)
            {
                isPressed = false;
            }
        }

        if (isSelected)
        {
            if (oilPercentage > 0)
            {
                oilPercentage -= Time.deltaTime * (100 / oilLifeInSec);

                if (oilPercentage <= reductionFactor)
                {
                    reduceIntensity -= lightReductionRate / 100;
                    reductionFactor -= lightReductionRate;
                    StartCoroutine(Reduce());
                }
            }

            gameManager.UpdateLightPercent(oilPercentage);
        }
        else
        {
            if (!isSelecting)
            {
                LanternLight.gameObject.SetActive(false);
                LanternGO.SetActive(false);
            }
        }

        oilPercentage = Mathf.Clamp(oilPercentage, 0, 100);
        reductionFactor = Mathf.Clamp(reductionFactor, 0, 100);
        LanternLight.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetColor("_TintColor", FlameTint);
    }

    IEnumerator Reduce()
    {
        while (LanternLight.intensity >= reduceIntensity)
        {
            LanternLight.intensity -= Time.deltaTime * 0.15f;
            FlameTint.a -= Time.deltaTime * 0.15f;
            yield return null;
        }

        LanternLight.intensity = (float)System.Math.Round(reduceIntensity, 2);
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>
        {
            {"lightPercentage", oilPercentage},
            {"lightIntensity", LanternLight.intensity},
            {"flameAlpha", FlameTint.a}
        };
    }

    public void OnLoad(Newtonsoft.Json.Linq.JToken token)
    {
        float lp = (float)token["lightPercentage"];
        float li = (float)token["lightIntensity"];
        float fa = (float)token["flameAlpha"];

        oilPercentage = lp;
        oldIntensity = li;
        FlameTint.a = fa;

        LanternLight.intensity = oldIntensity;
        reduceIntensity = oldIntensity;
        reductionFactor = oilPercentage - lightReductionRate;
    }
}

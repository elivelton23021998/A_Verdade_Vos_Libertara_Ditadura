﻿using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CCTV : MonoBehaviour, ISaveable
{
    public List<Camera> Cameras = new List<Camera>();

    [Space(7)]
    public Vector2Int renderTextureSize = new Vector2Int(512, 512);

    [Space(7)]
    public int channel = 0;
    public float antiSpam = 1;
    public bool enableNoise;

    [Header("Renders")]
    public MeshRenderer PowerButton;
    public MeshRenderer Display;

    [Header("Materiais")]
    public Material OffMaterial;
    public Material RenderMaterial;

    [Header("Sons")]
    public AudioClip OnSound;
    public AudioClip OffSound;
    public AudioClip ChangeChannelSound;

    private AudioSource audioSource;

    [Space(7)]
    public bool isOn;
    private bool canChange = true;

    private Camera renderCamera;
    private RenderTexture currentRender;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (Cameras.Count > 0)
        {
            foreach (var cam in Cameras)
            {
                cam.enabled = false;

                if (cam.GetComponent<NoiseAndGrain>())
                {
                    cam.GetComponent<NoiseAndGrain>().enabled = enableNoise;
                }
                else
                {
                    Debug.LogError("NoiseAndGrain script não foi encontrado em " + cam.transform.parent.parent.parent.gameObject.name);
                }
            }

            currentRender = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 16);
            currentRender.Create();
        }
        else
        {
            Debug.LogError("[CCTV] Atribua pelo menos uma câmera de CCTV.");
        }
    }

    void Update()
    {
        if (Cameras.Count == 0) return;

        renderCamera = Cameras[channel];
        renderCamera.enabled = true;

        if (currentRender)
        {
            renderCamera.targetTexture = currentRender;
        }

        if (isOn)
        {
            Display.material = RenderMaterial;
            PowerButton.material.EnableKeyword("_EMISSION");
            Display.material.SetTexture("_MainTex", currentRender);
        }
        else
        {
            foreach (var cam in Cameras)
            {
                cam.enabled = false;
            }

            Display.material = OffMaterial;
            PowerButton.material.DisableKeyword("_EMISSION");
        }
    }

    public void ChangeChannel()
    {
        if (isOn && canChange)
        {
            if (ChangeChannelSound) { AudioSource.PlayClipAtPoint(ChangeChannelSound, transform.position, 0.5f); }

            foreach (var cam in Cameras)
            {
                cam.enabled = false;
                cam.targetTexture = null;
            }

            channel = channel == Cameras.Count - 1 ? 0 : channel + 1;

            canChange = false;
            StartCoroutine(WaitChange());
        }
    }

    IEnumerator WaitChange()
    {
        yield return new WaitForSeconds(antiSpam);
        canChange = true;
    }

    public void TurnOnOff()
    {
        if (!isOn)
        {
            if (OnSound) {
                audioSource.clip = OnSound;
            }
        }
        else
        {
            if (OffSound)
            {
                audioSource.clip = OffSound;
            }
        }

        audioSource.Play();

        isOn = !isOn;
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>
        {
            {"isOn", isOn },
            {"channel", channel }
        };
    }

    public void OnLoad(JToken token)
    {
        channel = (int)token["channel"];
        isOn = (bool)token["isOn"];
    }
}

﻿using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class Radio : MonoBehaviour, ISaveable
{
    public AudioSource radioAudioSource;
    public AudioClip pushButton;

    public Renderer meshRenderer;
    public Color OnColor = Color.green;
    public Color OffColor = Color.red;

    [Header("Animation")]
    public Animation m_animation;
    public string OnAnimation;
    public string OffAnimation;
    public bool isOn;

    void Start()
    {
        meshRenderer.material.EnableKeyword("_EMISSION");
        radioAudioSource.loop = false;
        radioAudioSource.spatialBlend = 1f;

        if (isOn)
        {
            radioAudioSource.Play();
            meshRenderer.material.SetColor("_EmissionColor", OnColor);

            if (m_animation)
            {
                m_animation.Play(OnAnimation);
            }
        }
    }

    public void UseObject()
    {
        AudioSource.PlayClipAtPoint(pushButton, transform.position, 0.3f);

        if (!isOn)
        {
            radioAudioSource.Play();
            meshRenderer.material.SetColor("_EmissionColor", OnColor);

            if (m_animation)
            {
                m_animation.Play(OnAnimation);
            }

            isOn = true;
        }
        else
        {
            radioAudioSource.Pause();
            meshRenderer.material.SetColor("_EmissionColor", OffColor);

            if (m_animation)
            {
                m_animation.Play(OffAnimation);
            }

            isOn = false;
        }
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>()
        {
            { "isOn", isOn }
        };
    }

    public void OnLoad(JToken token)
    {
        isOn = token["isOn"].ToObject<bool>();

        if (isOn)
        {
            radioAudioSource.Play();
            meshRenderer.material.SetColor("_EmissionColor", OnColor);

            if (m_animation)
            {
                m_animation.Play(OnAnimation);
            }
        }
    }
}

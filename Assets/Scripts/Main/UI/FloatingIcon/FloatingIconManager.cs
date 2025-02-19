﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingIconManager : Singleton<FloatingIconManager>
{
    public class IconObjectPair
    {
        public GameObject FollowObject;
        public FloatingIcon Icon;

        public IconObjectPair(GameObject obj, FloatingIcon icon)
        {
            FollowObject = obj;
            Icon = icon;
        }
    }

    private AVVL_GameManager gameManager;

    [Header("Objetos de Ícone Flutuante")]
    public List<GameObject> FloatingIcons = new List<GameObject>();
    public List<IconObjectPair> FloatingIconCache = new List<IconObjectPair>();

    [Header("Raycasting")]
    public LayerMask Layer;

    [Header("UI")]
    public GameObject FloatingIconPrefab;
    public Transform FloatingIconUI;

    [Header("Propriedades")]
    public float followSmooth = 8;
    public float distanceShow = 3;
    public float distanceKeep = 4.5f;
    public float distanceRemove = 6;

    private GameObject Player;
    private GameObject Cam;

    void Awake()
    {
        gameManager = GetComponent<AVVL_GameManager>();
        Player = gameManager.Player;
        Cam = Tools.MainCamera().gameObject;
    }

    void Update()
    {
        if (FloatingIcons.Count > 0)
        {
            foreach (var obj in FloatingIcons)
            {
                if (Vector3.Distance(obj.transform.position, Player.transform.position) <= distanceShow)
                {
                    if (!ContainsFloatingIcon(obj) && IsObjectVisibleByCamera(obj))
                    {
                        AddFloatingIcon(obj);
                    }
                }
            }
        }

        if (FloatingIconCache.Count > 0)
        {
            for (int i = 0; i < FloatingIconCache.Count; i++)
            {
                IconObjectPair Pair = FloatingIconCache[i];

                if (Pair.FollowObject == null)
                {
                    Destroy(Pair.Icon.gameObject);
                    FloatingIconCache.RemoveAt(i);
                    return;
                }
                else if(Pair.FollowObject.GetComponent<Renderer>() && !Pair.FollowObject.GetComponent<Renderer>().enabled)
                {
                    Destroy(Pair.Icon.gameObject);
                    FloatingIconCache.RemoveAt(i);
                    return;
                }

                if (Vector3.Distance(Pair.FollowObject.transform.position, Player.transform.position) <= distanceKeep && IsObjectVisibleByCamera(Pair.FollowObject))
                {
                    Pair.Icon.OutOfDIstance(false);
                }
                else
                {
                    Pair.Icon.OutOfDIstance(true);
                }

                if (Vector3.Distance(Pair.FollowObject.transform.position, Player.transform.position) >= distanceRemove)
                {
                    Destroy(Pair.Icon.gameObject);
                    FloatingIconCache.RemoveAt(i);
                }

                if (Pair.FollowObject.GetComponent<InteractiveItem>())
                {
                    InteractiveItem interactiveItem = Pair.FollowObject.GetComponent<InteractiveItem>();

                    if (interactiveItem.floatingIconEnabled && !gameManager.isLocked)
                    {
                        Pair.Icon.SetIconVisible(true);
                    }
                    else
                    {
                        Pair.Icon.SetIconVisible(false);
                    }
                }
            }
        }
    }

    private void AddFloatingIcon(GameObject FollowObject)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(FollowObject.transform.position);
        GameObject icon = Instantiate(FloatingIconPrefab, screenPos, Quaternion.identity, FloatingIconUI);
        icon.GetComponent<FloatingIcon>().FollowObject = FollowObject;
        icon.transform.position = new Vector2(-20, -20);
        FloatingIconCache.Add(new IconObjectPair(FollowObject, icon.GetComponent<FloatingIcon>()));
    }

    private bool IsObjectVisibleByCamera(GameObject FollowObject)
    {
        if (Physics.Linecast(Cam.transform.position, FollowObject.transform.position, out RaycastHit hit, Layer))
        {
            if (hit.collider.gameObject == FollowObject && FollowObject.GetComponent<Renderer>().isVisible)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifique se o objeto está à distância
    /// </summary>
    public bool ContainsFloatingIcon(GameObject FollowObject)
    {
        return FloatingIconCache.Any(icon => icon.FollowObject == FollowObject);
    }

    /// <summary>
    /// Obter IconObject Pair por objeto
    /// </summary>
    public IconObjectPair GetIcon(GameObject FollowObject)
    {
        return FloatingIconCache.SingleOrDefault(icon => icon.FollowObject == FollowObject);
    }

    /// <summary>
    /// Definir o estado de visibilidade de FollowObject
    /// </summary>
    public void SetIconVisible(GameObject FollowObject, bool state)
    {
        FloatingIconCache.SingleOrDefault(icon => icon.FollowObject == FollowObject).Icon.SetIconVisible(state);
    }

    /// <summary>
    /// Definir o estado de visibilidade de todos os FloatingIcons
    /// </summary>
    public void SetAllIconsVisible(bool state)
    {
        if (FloatingIconCache.Count > 0)
        {
            foreach (var item in FloatingIconCache)
            {
                item.Icon.SetIconVisible(state);
            }
        }
    }
}

﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ContainerItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    private Inventory inventory;
    private Image background;

    [HideInInspector]
    public InventoryContainer inventoryContainer;
    public Item item;
    public int amount;

    [Header("UI")]
    public Image ItemSprite;
    public Text ItemTitle;
    public Text ItemCount;

    [Header("UI Cores")]
    public Color NormalColor = Color.white;
    public Color HoverColor = Color.white;
    public Color PressedColor = Color.white;
    public Color HoldColor = Color.white;

    private Color fadeColor;
    private bool hold;

    void Awake()
    {
        inventory = Inventory.Instance;
        background = GetComponent<Image>();
        background.color = NormalColor;
        fadeColor = NormalColor;
    }

    void Update()
    {
        if (item != null)
        {
            ItemSprite.sprite = item.itemSprite;
            ItemTitle.text = item.Title;
        }

        if(amount > 1)
        {
            ItemCount.text = "x" + amount;
        }
        else
        {
            ItemCount.text = string.Empty;
        }

        if (ItemSprite.sprite != null)
        {
            ItemSprite.enabled = true;
        }

        if(background.color != fadeColor)
        {
            background.color = Color.Lerp(background.color, fadeColor, Time.deltaTime * 12);
        }
    }

    public bool IsSelected()
    {
        return hold;
    }

    public void Deselect()
    {
        hold = false;
        fadeColor = NormalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        foreach (var item in inventory.ContainterItemsCache)
        {
            item.Deselect();
        }

        hold = true;
        fadeColor = HoldColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (hold)
        {
            return;
        }

        fadeColor = PressedColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hold)
        {
            return;
        }

        fadeColor = HoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hold)
        {
            fadeColor = HoldColor;
            return;
        }

        fadeColor = NormalColor;
    }
}

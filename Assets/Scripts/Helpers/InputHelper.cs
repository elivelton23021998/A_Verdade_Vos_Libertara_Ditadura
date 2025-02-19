﻿using UnityEngine;

public enum Axis { Forward, Backward, Left, Right }

/// <summary>
/// LerFornece métodos adicionais para Inputps
/// </summary>
public static class InputHelper
{

    /// <summary>
    /// Interpola linearmente o botão do teclado
    /// </summary>
    public static float GetKeyAxis(Axis axis, float from, bool lerpIn, float speed)
    {
        switch (axis)
        {
            case Axis.Forward:
                if (lerpIn)
                {
                    if (from < 0.9f)
                    {
                        return from += Time.deltaTime * speed;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    if (from > 0.1f)
                    {
                        return from -= Time.deltaTime * speed;
                    }
                    else
                    {
                        return 0;
                    }
                }
            case Axis.Backward:
                if (lerpIn)
                {
                    if (from > -0.9f)
                    {
                        return from -= Time.deltaTime * speed;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    if (from < -0.1f)
                    {
                        return from += Time.deltaTime * speed;
                    }
                    else
                    {
                        return 0;
                    }
                }
            case Axis.Left:
                if (lerpIn)
                {
                    if (from > -0.9f)
                    {
                        return from -= Time.deltaTime * speed;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    if (from < -0.1f)
                    {
                        return from += Time.deltaTime * speed;
                    }
                    else
                    {
                        return 0;
                    }
                }
            case Axis.Right:
                if (lerpIn)
                {
                    if (from < 0.9f)
                    {
                        return from += Time.deltaTime * speed;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    if (from > 0.1f)
                    {
                        return from -= Time.deltaTime * speed;
                    }
                    else
                    {
                        return 0;
                    }
                }
        }

        return 0;
    }
}
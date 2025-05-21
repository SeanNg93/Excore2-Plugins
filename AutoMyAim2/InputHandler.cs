﻿using System;
using System.Numerics;
using ExileCore2.Shared;

namespace AutoMyAim;

public class InputHandler
{
    private readonly Random _random = new();

    public bool IsValidClickPosition(Vector2 pos, RectangleF window)
    {
        var safeZone = GetSafeZone(window);

        return pos.X >= safeZone.Left &&
               pos.X <= safeZone.Right &&
               pos.Y >= safeZone.Top &&
               pos.Y <= safeZone.Bottom;
    }

    public Vector2 GetSafeAimPosition(Vector2 targetPos, RectangleF window)
    {
        var screenCenter = new Vector2(
            window.X + window.Width / 2,
            window.Y + window.Height / 2
        );

        var safeZone = GetSafeZone(window);

        if (!(targetPos.X < safeZone.Left) && !(targetPos.X > safeZone.Right) &&
            !(targetPos.Y < safeZone.Top) && !(targetPos.Y > safeZone.Bottom)) return targetPos;

        var vectorToTarget = targetPos - screenCenter;
        var normalizedVector = Vector2.Normalize(vectorToTarget);

        float scaleX = float.MaxValue, scaleY = float.MaxValue;

        if (normalizedVector.X != 0)
        {
            if (vectorToTarget.X > 0)
                scaleX = (safeZone.Right - screenCenter.X) / normalizedVector.X;
            else
                scaleX = (safeZone.Left - screenCenter.X) / normalizedVector.X;
        }

        if (normalizedVector.Y != 0)
        {
            if (vectorToTarget.Y > 0)
                scaleY = (safeZone.Bottom - screenCenter.Y) / normalizedVector.Y;
            else
                scaleY = (safeZone.Top - screenCenter.Y) / normalizedVector.Y;
        }

        var scale = Math.Min(scaleX, scaleY);
        return screenCenter + normalizedVector * scale;
    }

    public Vector2 GetRandomizedAimPosition(Vector2 targetPos, RectangleF window)
    {
        if (!AutoMyAim.Main.Settings.Render.Cursor.RandomizeInRadius)
            return targetPos;

        var attempts = 0;
        const int maxAttempts = 10;

        while (attempts < maxAttempts)
        {
            var angle = _random.NextDouble() * Math.PI * 2;
            var distance = _random.NextDouble() * AutoMyAim.Main.Settings.Render.Cursor.AcceptableRadius;

            var randomPos = new Vector2(
                targetPos.X + (float)(Math.Cos(angle) * distance),
                targetPos.Y + (float)(Math.Sin(angle) * distance)
            );

            if (IsValidClickPosition(randomPos, window))
                return randomPos;

            attempts++;
        }

        return targetPos;
    }

    private RectangleF GetSafeZone(RectangleF window)
    {
        var padding = AutoMyAim.Main.Settings.Render.Panels.PaddingPercentToCenter;
        return new RectangleF(
            window.X + window.Width * padding.Left.Value / 100f,
            window.Y + window.Height * padding.Top.Value / 100f,
            window.Width * (1 - (padding.Left.Value + padding.Right.Value) / 100f),
            window.Height * (1 - (padding.Top.Value + padding.Bottom.Value) / 100f)
        );
    }
}
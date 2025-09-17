using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

[BepInPlugin("com.vainstar.NINAH.WalkSpeed", "NINAH.WalkSpeed", "1.0.3")]
public class NINAHWalkSpeed : BasePlugin
{
    internal static ConfigFile Cfg;
    internal static ConfigEntry<float> SprintBonus;
    internal static ConfigEntry<KeyCode> SprintKey;
    internal static ConfigEntry<KeyCode> SpeedUpKey;
    internal static ConfigEntry<KeyCode> SpeedDownKey;

    public override void Load()
    {
        Cfg = Config;
        SprintBonus = Config.Bind("Movement", "SprintBonus", 2f, "Added to base walk speed while sprinting");
        SprintKey = Config.Bind("Input", "SprintKey", KeyCode.LeftShift, "Hold to sprint");
        SpeedUpKey = Config.Bind("Input", "IncreaseKey", KeyCode.Equals, "Increase sprint bonus");
        SpeedDownKey = Config.Bind("Input", "DecreaseKey", KeyCode.Minus, "Decrease sprint bonus");

        ClassInjector.RegisterTypeInIl2Cpp<Controller>();
        var go = new GameObject("NINAH.WalkSpeed_Controller");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Controller>();
    }

    public class Controller : MonoBehaviour
    {
        private ECM2.Character character;
        private float baseWalkSpeed = -1f;
        private string toastText = "";
        private float toastUntil = 0f;
        private float toastStart = 0f;
        private UnityEngine.GUIStyle toastStyle;
        private const float ToastDuration = 2f;
        private const float FadeDuration = 0.7f;

        private void Update()
        {
            if (character == null)
            {
                var root = GameObject.Find("PlayerInstance");
                if (root == null) return;
                var playerTf = root.transform.Find("Player");
                if (playerTf == null) return;
                character = playerTf.gameObject.GetComponent<ECM2.Character>();
                if (character == null) return;
                character.maxAcceleration = 999f;
                character._canEverCrouch = true;
                baseWalkSpeed = character._maxWalkSpeed;
            }

            if (global::UnityEngine.Input.GetKeyDown(NINAHWalkSpeed.SpeedUpKey.Value))
            {
                NINAHWalkSpeed.SprintBonus.Value += 1f;
                if (NINAHWalkSpeed.SprintBonus.Value < 0f) NINAHWalkSpeed.SprintBonus.Value = 0f;
                NINAHWalkSpeed.Cfg.Save();
                ShowToast($"Changed sprint speed to {baseWalkSpeed + NINAHWalkSpeed.SprintBonus.Value:0.##}");
            }
            if (global::UnityEngine.Input.GetKeyDown(NINAHWalkSpeed.SpeedDownKey.Value))
            {
                NINAHWalkSpeed.SprintBonus.Value = Math.Max(0f, NINAHWalkSpeed.SprintBonus.Value - 1f);
                NINAHWalkSpeed.Cfg.Save();
                ShowToast($"Changed sprint speed to {baseWalkSpeed + NINAHWalkSpeed.SprintBonus.Value:0.##}");
            }

            bool sprinting = global::UnityEngine.Input.GetKey(NINAHWalkSpeed.SprintKey.Value);
            float target = sprinting ? baseWalkSpeed + NINAHWalkSpeed.SprintBonus.Value : baseWalkSpeed;
            if (Math.Abs(character._maxWalkSpeed - target) > 0.0001f)
                character._maxWalkSpeed = target;
        }

        private void OnGUI()
        {
            if (UnityEngine.Time.time > toastUntil || string.IsNullOrEmpty(toastText)) return;
            if (toastStyle == null)
            {
                toastStyle = new UnityEngine.GUIStyle();
                toastStyle.fontSize = 54;
                toastStyle.fontStyle = FontStyle.Bold;
                toastStyle.normal.textColor = UnityEngine.Color.white;
            }

            float timeLeft = toastUntil - UnityEngine.Time.time;
            float alpha = 1f;
            if (timeLeft < FadeDuration) alpha = Mathf.Clamp01(timeLeft / FadeDuration);

            var content = new UnityEngine.GUIContent(toastText);
            var size = toastStyle.CalcSize(content);

            float x = 16f;
            float y = UnityEngine.Screen.height - size.y - 16f;

            var shadow = new UnityEngine.Rect(x + 2f, y + 2f, size.x, size.y);
            var rect = new UnityEngine.Rect(x, y, size.x, size.y);

            var white = new UnityEngine.Color(1f, 1f, 1f, alpha);
            var black = new UnityEngine.Color(0f, 0f, 0f, alpha * 0.75f);

            var old = toastStyle.normal.textColor;
            toastStyle.normal.textColor = black;
            UnityEngine.GUI.Label(shadow, content, toastStyle);
            toastStyle.normal.textColor = white;
            UnityEngine.GUI.Label(rect, content, toastStyle);
            toastStyle.normal.textColor = old;
        }

        private void ShowToast(string text)
        {
            toastText = text;
            toastStart = UnityEngine.Time.time;
            toastUntil = toastStart + ToastDuration;
        }
    }
}

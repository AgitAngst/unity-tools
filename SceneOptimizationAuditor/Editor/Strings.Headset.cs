namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        // The target headset on the Budgets tab.
        static void AddHeadset()
        {
            Add("ui.hs", "Headset", "Шлем");
            Add("ui.hs.tip", "Picking a headset fills the frame time and the transparent share with its values; physics, FPS colors and the field of view of viewpoints follow it.",
                "Выбор шлема заполняет время кадра и прозрачное в кадре его значениями; под него подстраиваются шаг физики, цвета FPS и поле зрения точек обзора.");
            Add("ui.hs.custom.tip", "The frame time or the transparent share differ from the values of the headset. \"Defaults\" brings them back.",
                "Время кадра или прозрачное в кадре отличаются от значений шлема. «По умолчанию» вернёт их.");
            Add("ui.hs.rate", "Refresh rate", "Частота");
            Add("ui.hs.rate.tip", "The rate set in SteamVR for the headset; the frame lasts 1000 / rate ms.", "Частота, выставленная шлему в SteamVR; кадр длится 1000 / частота мс.");
            Add("ui.hs.hz", "{0} Hz", "{0} Гц");
            Add("ui.hs.wireless", "The wireless adapter carries 90 Hz.", "Беспроводной адаптер передаёт 90 Гц.");
            Add("ui.hs.scale", "SteamVR resolution, %", "Разрешение SteamVR, %");
            Add("ui.hs.scale.tip", "Render resolution of SteamVR for the application, by area, as SteamVR shows it (20–500).",
                "Разрешение рендера SteamVR для приложения, по площади, как его показывает SteamVR (20–500).");
            Add("ui.hs.note", "Render ≈ {0} × {1} per eye · {2} Hz · frame {3} ms · pixels a second ×{4} against the Vive Pro at 90 Hz. The frame time, FPS colors, physics step, field of view of viewpoints and the transparent share follow the headset.",
                "Рендер ≈ {0} × {1} на глаз · {2} Гц · кадр {3} мс · пикселей в секунду ×{4} от Vive Pro на 90 Гц. От шлема зависят время кадра, цвета FPS, шаг физики, поле зрения точек обзора и прозрачное в кадре.");
            Add("ui.hs.context", "Headset: {0}, {1} Hz", "Шлем: {0}, {1} Гц");
            Add("ui.hs.context.edited", "Headset: {0}, {1} Hz, custom thresholds", "Шлем: {0}, {1} Гц, свои пороги");
        }
    }
}

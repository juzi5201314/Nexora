using UnityEngine;
using Verse;

namespace Nexora.ui.utils;

public static class Styles
{
    public static IDisposable TextAnchor(TextAnchor anchor) =>
        new Recover<TextAnchor>(Text.Anchor, anchor, textAnchor => Text.Anchor = textAnchor);
    public static IDisposable FontSize(GameFont font) =>
        new Recover<GameFont>(Text.Font, font, gameFont => Text.Font = gameFont);

    public static IDisposable GUIColor(Color color) =>
        new Recover<Color>(GUI.color, color, color0 => GUI.color = color0);

    public static IDisposable WordWrap(bool value) =>
        new Recover<bool>(Text.WordWrap, value, val => Text.WordWrap = val);

    private class Recover<T> : IDisposable
    {
        private readonly T _save;
        private readonly Action<T> _release;
        private bool _disposed;

        public Recover(T save, Action set, Action<T> release)
        {
            _save = save;
            _release = release;
            _disposed = false;
            set();
        }

        public Recover(T old, T @new, Action<T> set)
        {
            _save = old;
            _release = set;
            set(@new);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _release(_save);
        }
    }
}
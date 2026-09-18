using Rysy.Gui.Windows;
using Rysy.Helpers;
using System.Runtime.CompilerServices;

namespace Rysy.Components;

internal sealed class ComponentSafeInvoker<TComponent> where TComponent : class {
    private readonly ConditionalWeakTable<TComponent, TComponent> _thrownExceptions = new();
    private LangKey _titleLangId;

    public ComponentSafeInvoker(LangKey titleLangId) {
        _titleLangId = titleLangId;
    }

    public bool HasComponentCrashed(TComponent component) => _thrownExceptions.TryGetValue(component, out _);

    public void Invoke<TData>(TComponent component, TData data, Action<TComponent, TData> callback) {
        if (HasComponentCrashed(component))
            return;
        
        try {
            callback(component, data);
        } catch (Exception ex) {
            if (_thrownExceptions.TryAdd(component, component)) {
                _titleLangId.SetArg(0, component);
                
                PopupNotificationWindow.ShowException(_titleLangId, ex);
            }
        }
    }
}

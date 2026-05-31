using Rysy.Signals;

namespace Rysy.History;

internal sealed class HotswappableHistoryHandler : IHistoryHandler, ISignalEmitter {
    private IHistoryHandler _implementation;

    public HotswappableHistoryHandler(IHistoryHandler implementation) {
        SwapTo(implementation);
    }

    public void SwapTo(IHistoryHandler other) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_implementation is not null) {
            _implementation.UndoSimulations();
            _implementation.DisposeIfDisposable();
            
            _implementation.OnUndo -= OnImplementationUndo;
            _implementation.OnApply -= OnImplementationApply;
        }
        
        _implementation = other;
        _implementation.OnUndo += OnImplementationUndo;
        _implementation.OnApply += OnImplementationApply;
    }
    
    public Map Map {
        get => _implementation.Map;
        set => _implementation.Map = value;
    }

    SignalTarget ISignalEmitter.SignalTarget {
        get;
        set {
            field = value;
            if (_implementation is ISignalEmitter emitter) {
                emitter.SignalTarget = value;
            }
        }
    }

    private void OnImplementationUndo() {
        OnUndo?.Invoke();
    }
    
    private void OnImplementationApply() {
        OnApply?.Invoke();
    }

    public event Action? OnUndo;

    public event Action? OnApply;

    public void UndoSimulations() {
        _implementation.UndoSimulations();
    }

    public void ApplyNewSimulation(IHistoryAction? action) {
        _implementation.ApplyNewSimulation(action);
    }

    public void ApplyNewAction(IEnumerable<IHistoryAction?> actions) {
        _implementation.ApplyNewAction(actions);
    }

    public void ApplyNewAction(IHistoryAction? action) {
        _implementation.ApplyNewAction(action);
    }

    public void Undo() {
        _implementation.Undo();
    }

    public void Redo() {
        _implementation.Redo();
    }

    public void Clear() {
        _implementation.Clear();
    }
}
using Rysy.Components;
using Rysy.Signals;

namespace Rysy.History;

internal sealed class HotswappableHistoryHandler : IHistoryHandler, ISignalEmitter {
    private IHistoryHandler _implementation;

    private readonly SignalWrapper _signalWrapper;

    private class SignalWrapper(IHistoryHandler treatAs) : ISignalListener, ISignalEmitter {
        public void OnSignal<T>(T signal) where T : ISignal {
            switch (signal)
            {
                case HistoryChanged:
                    SignalTarget.Send(new HistoryChanged(treatAs));
                    return;
                case HistoryActionApplied applied:
                    SignalTarget.Send(applied with { Handler = treatAs });
                    return;
                case HistoryActionUndone undone:
                    SignalTarget.Send(undone with { Handler = treatAs });
                    return;
                case HistoryActionSimulationApplied applied:
                    SignalTarget.Send(applied with { Handler = treatAs });
                    return;
                case HistoryActionSimulationUndone undone:
                    SignalTarget.Send(undone with { Handler = treatAs });
                    return;
                default:
                    SignalTarget.Send(signal);
                    break;
            }
        }

        public SignalTarget SignalTarget { get; set; }
    }

    public HotswappableHistoryHandler(IHistoryHandler implementation) {
        _signalWrapper = new(this);
        
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
            _signalWrapper.SignalTarget = value;
            if (_implementation is ISignalEmitter emitter) {
                emitter.SignalTarget = SignalTarget.From(_signalWrapper);
            }
        }
    }

    private void OnImplementationUndo() {
        OnUndo?.Invoke();
    }
    
    private void OnImplementationApply() {
        OnApply?.Invoke();
    }

    public IHistoryAction? MostRecentAction => _implementation.MostRecentAction;
    
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
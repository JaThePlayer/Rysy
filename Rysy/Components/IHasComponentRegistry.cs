using Rysy.Signals;

namespace Rysy.Components;

public interface IHasComponentRegistry : ISignalListener<SelfAdded>, ISignalListener<SelfRemoved> {
    public IComponentRegistry? Registry { get; set; }
    
    void ISignalListener<SelfAdded>.OnSignal(SelfAdded signal) {
        Registry = signal.Registry;
    }

    void ISignalListener<SelfRemoved>.OnSignal(SelfRemoved signal) {
        Registry = null;
    }
}
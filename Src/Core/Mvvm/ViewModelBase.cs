using ReactiveUI;
using System;
using System.Reactive.Subjects;

namespace ProjektSlowkaRemasterd.Src.Core.Mvvm;

/// <summary>
/// Simple base ViewModel without complex state management.
/// </summary>
public class ViewModelBase : ReactiveObject
{
}

/// <summary>
/// Base ViewModel supporting the MVI-like state pattern with immutable state.
/// </summary>
/// <typeparam name="TState">Immutable record representing the state</typeparam>
public class ViewModelBase<TState> : ViewModelBase where TState : class
{
    private readonly BehaviorSubject<TState> _stateSubject;

    /// <summary>
    /// Current immutable state.
    /// </summary>
    public TState State => _stateSubject.Value;

    /// <summary>
    /// Observable sequence of state changes.
    /// </summary>
    public IObservable<TState> StateObservable => _stateSubject;

    protected ViewModelBase(TState initialState)
    {
        _stateSubject = new BehaviorSubject<TState>(initialState);
    }

    /// <summary>
    /// Updates the state using an immutable transformation.
    /// </summary>
    /// <param name="updateFunc">Transformation function</param>
    protected void UpdateState(Func<TState, TState> updateFunc)
    {
        var newState = updateFunc(_stateSubject.Value);
        _stateSubject.OnNext(newState);
        this.RaisePropertyChanged(nameof(State));
    }
}

namespace YarnSpinnerGodot;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

public partial struct YarnTaskMethodBuilder
{
    private AsyncTaskMethodBuilder methodBuilder;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static private AsyncTaskMethodBuilder GetBuilder() => AsyncTaskMethodBuilder.Create();
}

public partial struct YarnTaskMethodBuilder<T>
{
    private AsyncTaskMethodBuilder<T> methodBuilder;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static private AsyncTaskMethodBuilder<T> GetBuilder() => AsyncTaskMethodBuilder<T>.Create();
}

public partial struct YarnTaskMethodBuilder
{
    // 1. Static Create method.
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YarnTaskMethodBuilder Create()
    {
        YarnTaskMethodBuilder result = default;
        result.methodBuilder = GetBuilder();
        return result;
    }

    // 2. TaskLike Task property.
    public YarnTask Task
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => methodBuilder.Task;
    }

    // 3. SetException
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception) => methodBuilder.SetException(exception);

    // 4. SetResult
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult() => methodBuilder.SetResult();

    // 5. AwaitOnCompleted
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine => methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    // 6. AwaitUnsafeOnCompleted
    [DebuggerHidden]
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine => methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

    // 7. Start
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine => methodBuilder.Start(ref stateMachine);

    // 8. SetStateMachine
    [DebuggerHidden]
    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        methodBuilder.SetStateMachine(stateMachine);
    }
}

public partial struct YarnTaskMethodBuilder<T>
{
    // 1. Static Create method.
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YarnTaskMethodBuilder<T> Create()
    {
        YarnTaskMethodBuilder<T> result = default;
        result.methodBuilder = GetBuilder();
        return result;
    }

    // 2. TaskLike Task property.
    public YarnTask<T> Task
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => methodBuilder.Task;
    }

    // 3. SetException
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception)
    {
        methodBuilder.SetException(exception);
    }

    // 4. SetResult
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult(T result) => methodBuilder.SetResult(result);

    // 5. AwaitOnCompleted
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine => methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    // 6. AwaitUnsafeOnCompleted
    [DebuggerHidden]
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine => methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

    // 7. Start
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine => methodBuilder.Start(ref stateMachine);

    // 8. SetStateMachine
    [DebuggerHidden]
    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        methodBuilder.SetStateMachine(stateMachine);
    }
}
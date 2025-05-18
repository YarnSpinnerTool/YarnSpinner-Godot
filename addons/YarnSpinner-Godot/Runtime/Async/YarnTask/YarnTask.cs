#define YARNTASKS_ARE_SYSTEMTASKS


using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace YarnSpinnerGodot;

public interface IYarnTask
{
    bool IsCompleted();
    bool IsCompletedSuccessfully();
    void Forget();
}

[AsyncMethodBuilder(typeof(YarnTaskMethodBuilder))]
public partial struct YarnTask { }

[AsyncMethodBuilder(typeof(YarnTaskMethodBuilder<>))]
public partial struct YarnTask<T>
{
    public static partial YarnTask<T> FromResult(T value);
}

public partial class YarnTaskCompletionSource
{
    public partial bool TrySetResult();
    public partial bool TrySetException(System.Exception exception);
    public partial bool TrySetCanceled();
}

public partial class YarnTaskCompletionSource<T>
{
    public partial bool TrySetResult(T value);
    public partial bool TrySetException(System.Exception exception);
    public partial bool TrySetCanceled();
}

// Static implementation-dependent utility methods
public partial struct YarnTask
{
    public static partial YarnTask WaitUntilCanceled(System.Threading.CancellationToken token);

    /// <summary>
    /// Creates a <see cref="YarnTask"/> that delays for the time indicated
    /// by <paramref name="timeSpan"/>, and then returns.
    /// </summary>
    /// <param name="timeSpan">The amount of time to wait.</param>
    /// <param name="token">A token that can be used to cancel the
    /// task.</param>
    /// <returns>A new <see cref="YarnTask"/>.</returns>
    public static partial YarnTask Delay(TimeSpan timeSpan, CancellationToken token = default);
    public static YarnTask Delay(int milliseconds, CancellationToken token = default) => Delay(TimeSpan.FromMilliseconds(milliseconds), token);
    public static partial YarnTask WaitUntil(System.Func<bool> predicate, System.Threading.CancellationToken token = default);
    public static partial YarnTask Yield();

    public static partial YarnTask WhenAll(params YarnTask[] tasks);
    public static partial YarnTask WhenAll(IEnumerable<YarnTask> tasks);
    public static partial YarnTask<T[]> WhenAll<T>(params YarnTask<T>[] tasks);
    public static partial YarnTask<T[]> WhenAll<T>(IEnumerable<YarnTask<T>> tasks);

    public readonly partial YarnTask<bool> SuppressCancellationThrow();

    public static YarnTask<T> FromResult<T>(T value) => YarnTask<T>.FromResult(value);
}

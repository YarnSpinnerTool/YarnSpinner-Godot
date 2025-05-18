using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace YarnSpinnerGodot;

public partial struct YarnTask
{
    public TaskAwaiter GetAwaiter() => Task.GetAwaiter();

    Task Task;
    readonly public bool IsCompleted() => Task.IsCompleted;
    readonly public bool IsCompletedSuccessfully() => Task.IsCompletedSuccessfully;

    public static implicit operator Task(YarnTask YarnTask)
    {
        return YarnTask.Task;
    }

    public static implicit operator YarnTask(Task task)
    {
        return new YarnTask {Task = task};
    }

    readonly public async void Forget()
    {
        try
        {
            await Task;
        }
        catch (System.Exception e)
        {
            GD.PushError(e);
        }
    }

    public static partial async YarnTask WaitUntilCanceled(System.Threading.CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await DefaultActions.Wait(0.01);
        }
    }

    public static YarnTask CompletedTask => Task.CompletedTask;

    /// <summary>
    /// Creates a <see cref="YarnTask"/> that delays for the time indicated
    /// by <paramref name="timeSpan"/>, and then returns.
    /// </summary>
    /// <param name="timeSpan">The amount of time to wait.</param>
    /// <param name="token">A token that can be used to cancel the
    /// task.</param>
    /// <returns>A new <see cref="YarnTask"/>.</returns>
    public static partial YarnTask Delay(TimeSpan timeSpan, CancellationToken token)
    {
        return Task.Delay(timeSpan, token);
    }

    public static partial async YarnTask WaitUntil(System.Func<bool> predicate,
        System.Threading.CancellationToken token)
    {
        while (!token.IsCancellationRequested && predicate() == false)
        {
            await NextFrame();
        }
    }

    public static partial async YarnTask Yield()
    {
        await NextFrame();
    }

    public static partial YarnTask WhenAll(params YarnTask[] tasks)
    {
        return WhenAll((IEnumerable<YarnTask>) tasks);
    }

    public static partial async YarnTask WhenAll(IEnumerable<YarnTask> tasks)
    {
        // Don't love this allocation here; try and find a better approach
        List<Task> taskList = new List<Task>();
        foreach (var task in tasks)
        {
            taskList.Add(task);
        }

        await Task.WhenAll(taskList.ToArray());
    }

    public static async partial YarnTask<T[]> WhenAll<T>(params YarnTask<T>[] tasks)
    {
        return await Task.WhenAll(Array.ConvertAll<YarnTask<T>, Task<T>>(tasks, t => t));
    }

    public static async partial YarnTask<T[]> WhenAll<T>(IEnumerable<YarnTask<T>> tasks)
    {
        var uniTasks = new List<Task<T>>();
        foreach (var task in tasks)
        {
            uniTasks.Add(task);
        }

        return await Task.WhenAll(uniTasks);
    }

    public readonly async partial YarnTask<bool> SuppressCancellationThrow()
    {
        try
        {
            await Task;
        }
        catch (OperationCanceledException)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Wait for the next process frame.
    /// </summary>
    public static async Task NextFrame()
    {
        var mainLoop = (SceneTree) Engine.GetMainLoop();
        await (mainLoop).ToSignal(mainLoop, "process_frame");
    }

#if USE_ADDRESSABLES
        public static partial async YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken)
        {
            await operationHandle.Task;
        }

        public static partial async YarnTask<T> WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken)
        {
            return await operationHandle.Task;
        }
#endif
}

public partial struct YarnTask<T>
{
    Task<T> Task;
    public TaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();

    readonly public bool IsCompleted() => Task.IsCompleted;
    readonly public bool IsCompletedSuccessfully() => Task.IsCompletedSuccessfully;

    public static implicit operator Task<T>(YarnTask<T> YarnTask)
    {
        return YarnTask.Task;
    }

    public static implicit operator YarnTask<T>(Task<T> task)
    {
        return new YarnTask<T> {Task = task};
    }

    public static partial YarnTask<T> FromResult(T value)
    {
        return Task<T>.FromResult(value);
    }

    readonly public void Forget()
    {
    }
}

public partial class YarnTaskCompletionSource
{
    private TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();

    public partial bool TrySetResult()
    {
        return taskCompletionSource.TrySetResult(1);
    }

    public partial bool TrySetException(System.Exception exception)
    {
        return taskCompletionSource.TrySetException(exception);
    }

    public partial bool TrySetCanceled()
    {
        return taskCompletionSource.TrySetCanceled();
    }

    public YarnTask Task => taskCompletionSource.Task;
}

public partial class YarnTaskCompletionSource<T>
{
    private TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();

    public partial bool TrySetResult(T value)
    {
        return taskCompletionSource.TrySetResult(value);
    }

    public partial bool TrySetException(System.Exception exception)
    {
        return taskCompletionSource.TrySetException(exception);
    }

    public partial bool TrySetCanceled()
    {
        return taskCompletionSource.TrySetCanceled();
    }

    public YarnTask<T> Task => taskCompletionSource.Task;
}
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder.Configuration;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Kiota.Builder.Tests;

// Regression coverage for the cross-thread deadlock that can occur when two Parallel.ForEach
// workers each build a class whose parent is, transitively, waiting on the other worker's class.
// KiotaBuilder detects that cycle in its wait-for graph instead of blocking forever; these tests
// exercise that detection directly rather than trying to race real threads into the exact
// interleaving, which the original bug depended on and which large specs like Graph triggered.
public sealed class ModelClassBuildDeadlockAvoidanceTests
{
    private static KiotaBuilder NewBuilder() =>
        new(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration { ClientClassName = "Test", OpenAPIFilePath = "test.yaml" }, new HttpClient());

    private static object GetPrivateField(KiotaBuilder builder, string name) =>
        typeof(KiotaBuilder).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(builder)!;

    private static bool WouldWaitingCreateCycle(KiotaBuilder builder, ModelClassBuildLifecycle target, int currentThreadId) =>
        (bool)typeof(KiotaBuilder).GetMethod("WouldWaitingCreateCycle", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(builder, [target, currentThreadId])!;

    private static void WaitForSuperClassProperties(KiotaBuilder builder, ModelClassBuildLifecycle lifecycle, int currentThreadId, string declarationName, string namespaceName) =>
        typeof(KiotaBuilder).GetMethod("WaitForSuperClassProperties", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(builder, [lifecycle, currentThreadId, declarationName, namespaceName]);

    [Fact]
    public void DetectsTwoThreadCycle()
    {
        var builder = NewBuilder();
        var owners = (ConcurrentDictionary<ModelClassBuildLifecycle, int>)GetPrivateField(builder, "lifecycleOwnerThreadId");
        var waiting = (ConcurrentDictionary<int, ModelClassBuildLifecycle>)GetPrivateField(builder, "threadWaitingOnLifecycle");

        using var lifecycleX = new ModelClassBuildLifecycle();
        using var lifecycleY = new ModelClassBuildLifecycle();
        // Thread 1 owns X and is already blocked waiting on Y (owned by thread 2).
        owners[lifecycleX] = 1;
        owners[lifecycleY] = 2;
        waiting[1] = lifecycleY;

        // Thread 2 about to wait on X would close the cycle back to itself.
        Assert.True(WouldWaitingCreateCycle(builder, lifecycleX, 2));
    }

    [Fact]
    public void DetectsLongerCycleAcrossMultipleThreads()
    {
        var builder = NewBuilder();
        var owners = (ConcurrentDictionary<ModelClassBuildLifecycle, int>)GetPrivateField(builder, "lifecycleOwnerThreadId");
        var waiting = (ConcurrentDictionary<int, ModelClassBuildLifecycle>)GetPrivateField(builder, "threadWaitingOnLifecycle");

        using var lifecycleA = new ModelClassBuildLifecycle();
        using var lifecycleB = new ModelClassBuildLifecycle();
        using var lifecycleC = new ModelClassBuildLifecycle();
        // thread 1 owns A, waits on B (owned by thread 2); thread 2 waits on C (owned by thread 3).
        owners[lifecycleA] = 1;
        owners[lifecycleB] = 2;
        owners[lifecycleC] = 3;
        waiting[1] = lifecycleB;
        waiting[2] = lifecycleC;

        // thread 3 about to wait on A would close a 3-thread cycle.
        Assert.True(WouldWaitingCreateCycle(builder, lifecycleA, 3));
    }

    [Fact]
    public void DoesNotFlagIndependentModelsAsCyclic()
    {
        var builder = NewBuilder();
        var owners = (ConcurrentDictionary<ModelClassBuildLifecycle, int>)GetPrivateField(builder, "lifecycleOwnerThreadId");

        using var lifecycleX = new ModelClassBuildLifecycle();
        using var lifecycleY = new ModelClassBuildLifecycle();
        owners[lifecycleX] = 1;
        owners[lifecycleY] = 2;
        // Thread 2 isn't waiting on anything owned by thread 3: no cycle.
        Assert.False(WouldWaitingCreateCycle(builder, lifecycleY, 3));
    }

    [Fact]
    public void DoesNotFlagUnownedOrCompletedLifecycleAsCyclic()
    {
        var builder = NewBuilder();
        using var lifecycleX = new ModelClassBuildLifecycle();
        // Nobody currently owns lifecycleX (not started, or already finished): always safe to wait.
        Assert.False(WouldWaitingCreateCycle(builder, lifecycleX, 1));
    }

    [Fact]
    public void SkipsWaitingWhenCycleDetectedInsteadOfBlocking()
    {
        var builder = NewBuilder();
        var owners = (ConcurrentDictionary<ModelClassBuildLifecycle, int>)GetPrivateField(builder, "lifecycleOwnerThreadId");
        var waiting = (ConcurrentDictionary<int, ModelClassBuildLifecycle>)GetPrivateField(builder, "threadWaitingOnLifecycle");

        using var lifecycleX = new ModelClassBuildLifecycle();
        using var lifecycleY = new ModelClassBuildLifecycle();
        owners[lifecycleX] = 1;
        owners[lifecycleY] = 2;
        waiting[1] = lifecycleY;

        // lifecycleX's properties are never signaled: a real wait would block forever.
        // Because thread 2 waiting on X closes the cycle, this must return promptly instead.
        WaitForSuperClassProperties(builder, lifecycleX, 2, "Child", "Ns");
        Assert.False(lifecycleX.IsPropertiesBuilt());
    }

    [Fact(Timeout = 5000)]
    public async Task StillWaitsForRealParentCompletionWhenNotCyclic()
    {
        var builder = NewBuilder();
        using var lifecycleParent = new ModelClassBuildLifecycle();

        // Monitor.Enter/Exit are thread-affine, so the owner's start and completion must run as a
        // single synchronous unit on one pool thread rather than straddling an `await`.
        var ownerTask = Task.Run(() =>
        {
            lifecycleParent.StartBuildingProperties();
            Thread.Sleep(300);
            lifecycleParent.PropertiesBuildingDone();
        }, TestContext.Current.CancellationToken);

        var waitTask = Task.Run(() => WaitForSuperClassProperties(builder, lifecycleParent, Environment.CurrentManagedThreadId + 1000, "Child", "Ns"), TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.False(waitTask.IsCompleted);

        await Task.WhenAll(ownerTask, waitTask);
        Assert.True(lifecycleParent.IsPropertiesBuilt());
    }
}

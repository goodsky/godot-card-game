using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public struct CoroutineDelay
{
    public double TimeSec { get; private set; }

    public CoroutineDelay(double delay)
    {
        TimeSec = delay;
    }
}

public static class NodeExtensions
{
    public static SignalAwaiter Delay(this Node node, double timeSec)
    {
        SceneTree scene = node.GetTree();
        SceneTreeTimer timer = scene.CreateTimer(timeSec);
        return node.ToSignal(timer, "timeout");
    }

    public static async Task StartCoroutine(this Node node, IEnumerable coroutine, CancellationToken? token = null)
    {
        SceneTree scene = node.GetTree();
        foreach (object x in coroutine)
        {
            if (x is CoroutineDelay xDelay)
            {
                await node.Delay(xDelay.TimeSec);
            }
            else
            {
                await node.ToSignal(scene, SceneTree.SignalName.PhysicsFrame);
            }

            if (token != null && token.Value.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
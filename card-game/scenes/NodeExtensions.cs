using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public struct CoroutineDelay
{
    public double TimeSec { get; private set; }

    public CoroutineDelay(double delaySec)
    {
        TimeSec = delaySec;
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

    public static Task StartCoroutine(this Node node, IEnumerable coroutine, CancellationToken? token = null)
    {
        return StartCoroutineInternal(node, coroutine, token).ContinueWith((t) =>
        {
            if (t.IsFaulted)
            {
                GD.PushError($"Unhandled exception in coroutine! Ex={t.Exception}");
            }
        });
    }

    private static async Task StartCoroutineInternal(Node node, IEnumerable coroutine, CancellationToken? token = null)
    {
        SceneTree scene = node.GetTree();
        foreach (object x in coroutine)
        {
            if (x is CoroutineDelay xDelay)
            {
                await node.Delay(xDelay.TimeSec);
            }
            else if (x is IEnumerable xEnumerable)
            {
                await StartCoroutineInternal(node, xEnumerable, token);
            }
            else if (x is Task xTask)
            {
                await xTask;
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

    public static IEnumerable LerpGlobalPositionCoroutine(this Node2D node, Vector2 target, float speed)
    {
        Card card = node as Card;
        card?.SetAnimationControl(true);

        Vector2 start = node.GlobalPosition;
        for (float t = 0.0f; t < 1.0f; t = Mathf.Clamp(t + speed, 0.0f, 1.0f))
        {
            node.GlobalPosition = start.Lerp(target, t);
            yield return null;
        }

        node.GlobalPosition = target;
        card?.SetAnimationControl(false);
    }

    public static IEnumerable FadeTo(this CanvasItem node, float endAlpha, float? startAlpha = null, float speed = 0.05f)
    {
        Color nodeColor = node.Modulate;
        float startA = startAlpha ?? nodeColor.A;
        float deltaA = endAlpha - startA;
        for (float t = 0.0f; t < 1.0f; t = Mathf.Clamp(t + speed, 0.0f, 1.0f))
        {
            nodeColor.A = startA + (t * deltaA);
            node.Modulate = nodeColor;
            yield return null;
        }

        nodeColor.A = endAlpha;
        node.Modulate = nodeColor;
    }
}
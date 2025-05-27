using System.Collections.Generic;

namespace Fade.MonoGame.Game;

public class BaseSystem<T>
{
    public static List<T> items;
    private static Dictionary<int, int> _map;
}
namespace CamelCavalry
{
    internal static class CamelLog
    {
        internal static void Print(string message)
        {
            TaleWorlds.Library.Debug.Print("[CamelCavalry] " + message);
        }
    }
}

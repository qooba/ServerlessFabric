namespace Qooba.ServerlessFabric
{
    internal static class ActorMethodHelper
    {
        public static string PrepareMethodQueryString(string methodName, string requestName, string responseName)
        {
            return $"{methodName}_{requestName}_{responseName}";
        }
    }
}

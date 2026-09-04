namespace Murim.World;

internal static class DeterministicRandomSeed
{
    public static int Create(int worldSeed, long day, params Guid[] ids)
    {
        unchecked
        {
            uint hash = 2166136261u;
            hash = Mix(hash, (uint)worldSeed);
            hash = Mix(hash, (uint)day);
            hash = Mix(hash, (uint)(day >> 32));

            foreach (var id in ids)
            {
                foreach (var b in id.ToByteArray())
                    hash = (hash ^ b) * 16777619u;
            }

            hash ^= hash >> 16;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            return (int)hash;
        }
    }

    private static uint Mix(uint hash, uint value)
    {
        hash ^= value;
        return hash * 16777619u;
    }
}

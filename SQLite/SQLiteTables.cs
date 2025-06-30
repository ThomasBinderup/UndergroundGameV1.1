using SQLite;
using UnityEngine;

namespace SQLiteTables
{
    public class Resource
    {
        [PrimaryKey, AutoIncrement]
        public int ResourceId { get; set; }
        public int ResourceTypeId { get; set; }

        // Store position as 3 separate floats
        public float WorldPositionX { get; set; }
        public float WorldPositionY { get; set; }
        public float WorldPositionZ { get; set; }

        // Store quaternion as 4 floats
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public float RotationW { get; set; }

        // Scale
        public float Scale { get; set; }

        public bool IsActive { get; set; }
        public long DestroyedAt { get; set; }
        public float RespawnTime_Seconds { get; set; }
    }

    public class ServerInfo
    {
        [PrimaryKey, AutoIncrement]
        public int PrimaryId { get; set; }
        public long ServerAge { get; set; } // in seconds
        public int NextAvailableItemId { get; set; }
    }
}

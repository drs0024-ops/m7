namespace Game.Gameplay.Save
{
    /// <summary>
    /// Handles version-to-version save data migration.
    /// Each block upgrades data from version N to N+1.
    /// Add new blocks at the bottom as the save format evolves.
    /// </summary>
    public static class SaveMigration
    {
        /// <summary>
        /// Migrates save data from <paramref name="fromVersion"/> to <paramref name="toVersion"/>.
        /// </summary>
        public static void Migrate(SaveDataContainer data, int fromVersion, int toVersion)
        {
            int v = fromVersion;

            // v0 → v1: Initial versioning. Old saves have no version field (deserializes as 0).
            // No data transformation needed — all existing keys remain valid.
            if (v < 1)
            {
                // Future: if a key was renamed or a new required key was added, handle it here.
                // Example:
                // if (data.TryGetValue("OldKeyName", out var val))
                // {
                //     data.SetValue("NewKeyName", val);
                // }
            }

            // v1 → v2: (future migration goes here)
            // if (v < 2)
            // {
            //     // ...
            // }

            data.version = toVersion;
        }
    }
}   
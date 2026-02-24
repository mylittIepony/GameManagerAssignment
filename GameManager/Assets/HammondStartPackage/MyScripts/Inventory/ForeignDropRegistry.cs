
public static class ForeignDropRegistry
{
    const string CountKey = "ForeignDrops/Count";

    public static void Add(string id)
    {

        int count = SaveManager.GetInt(CountKey, 0);
        for (int i = 0; i < count; i++)
            if (SaveManager.Get($"ForeignDrops/{i}", "") == id) return;

        SaveManager.Set($"ForeignDrops/{count}", id);
        SaveManager.SetInt(CountKey, count + 1);
    }

    public static void Remove(string id)
    {
        int count = SaveManager.GetInt(CountKey, 0);
        for (int i = 0; i < count; i++)
        {
            if (SaveManager.Get($"ForeignDrops/{i}", "") == id)
            {

                string last = SaveManager.Get($"ForeignDrops/{count - 1}", "");
                SaveManager.Set($"ForeignDrops/{i}", last);
                SaveManager.DeleteKey($"ForeignDrops/{count - 1}");
                SaveManager.SetInt(CountKey, count - 1);
                SaveManager.DeleteByPrefix($"ForeignDrop/WorldItem/{id}");
                return;
            }
        }
    }

    public static System.Collections.Generic.List<string> GetAll()
    {
        int count = SaveManager.GetInt(CountKey, 0);
        var list = new System.Collections.Generic.List<string>(count);
        for (int i = 0; i < count; i++)
        {
            string id = SaveManager.Get($"ForeignDrops/{i}", "");
            if (!string.IsNullOrEmpty(id)) list.Add(id);
        }
        return list;
    }
}
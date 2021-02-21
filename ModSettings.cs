using System;

namespace Modding
{
    [Serializable]
    public class ModSettings
    {
        public SerializableIntDictionary IntValues = new();
        public SerializableBoolDictionary BoolValues = new();
        public SerializableFloatDictionary FloatValues = new();
        public SerializableStringDictionary StringValues = new();
    }

    [Serializable]
    public class ModSettingsDictionary : SerializableDictionary<string, ModSettings> { }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modding
{
    [Serializable]
    public class SerializableList<T> : List<T>
    {
        [SerializeField]
        private List<T> _values;

        public SerializableList()
        {
            _values = this;
        }
    }

    [Serializable]
    public class SerializableIntList : SerializableList<int> { }

    [Serializable]
    public class SerializableBoolList : SerializableList<bool> { }

    [Serializable]
    public class SerializableFloatList : SerializableList<float> { }

    [Serializable]
    public class SerializableStringList : SerializableList<string> { }
}

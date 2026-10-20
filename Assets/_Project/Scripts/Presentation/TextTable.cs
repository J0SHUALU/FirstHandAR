using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using FirstHand.Assets;
using FirstHand.Core;
using FirstHand.Data;

namespace FirstHand.Presentation
{

    [CreateAssetMenu(menuName = "FirstHand/Text Table", fileName = "Text_en")]
    public class TextTable : ScriptableObject
    {
        [Serializable] public struct Entry { public string key; [TextArea] public string text; }
        public Language language;
        public List<Entry> entries = new List<Entry>();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Editor
{
    [CreateAssetMenu(fileName = "SheetImporter", menuName = "ScriptableObjects/Editor/SheetImporter")]
    public class SheetImporterSettings : ScriptableObject
    {
        [System.Serializable]
        public class SheetPathData
        {
            public string Name = "시트 이름";
            public string Address = "";
            public SheetPathData(string name, string address)
            {
                Name = name;
                Address = address;
            }
        }

        //public string GenerateScriptPath;
        public string jsonPath;
        public string messagePackPath;
        public string localExcelPath;
        
        public List<SheetPathData> sheetPathData;
    }
}

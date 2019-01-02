using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;

public struct UiInfo : ISharedComponentData
{
    public TextMeshProUGUI CurrentGenerationInfo;

    public TextMeshProUGUI CurrentGenotypeInfo;

    public TextMeshProUGUI PrevGenerationInfo;
}

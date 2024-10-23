using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEngineHandler
{
    LogicEngine GetEngine();
    Unit GetOwner();
    void SetOwner (Unit owner);

    Object GetData();
}

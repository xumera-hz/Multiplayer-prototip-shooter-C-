using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitContainer : MonoBehaviour, IOwner {

    public GameObject GO { get; private set; }
    public Transform TF { get; private set; }
    public MoveControl MoveControl { get; private set; }
    public LifeControl LifeControl { get; private set; }
    public UnitWeaponControl WeaponControl { get; private set; }
    public Unit UnitControl { get; private set; }
    public AnimatorWrapper Anim { get; private set; }
    public UnitVisible VisibleControl { get; private set; }
    public CharacterController CharacterControl { get; private set; }
    public UnitReceiver Receiver { get; private set; }

    public TypeOwner GetTypeOwner { get { return TypeOwner.Character; } }

    public GameObject GameObj { get { return GO; } }

    //TODO: тут группы проверять на friedly fire
    public bool CheckIgnoreObject(Transform obj)
    {
        //В самого себя стрелять не надо))
        if (obj.IsNullOrDestroy()) return false;
        if (obj == TF) return true;
        var unit = obj.GetComponent<UnitContainer>();
        return !unit.IsNullOrDestroy() && unit == this;
    }

    public IAnchor Anchor { get { return null; } }

    void Awake ()
    {
        GO = gameObject;
        TF = transform;
        MoveControl = GetComponentInChildren<MoveControl>();
        LifeControl = GetComponentInChildren<LifeControl>();
        WeaponControl = GetComponentInChildren<UnitWeaponControl>();
        UnitControl = GetComponentInChildren<Unit>();
        Anim = GetComponentInChildren<CharacterAnimatorWrapper>();
        VisibleControl = GetComponentInChildren<UnitVisible>();
        CharacterControl = GetComponentInChildren<CharacterController>();
        Receiver = GetComponentInChildren<UnitReceiver>();
    }
}

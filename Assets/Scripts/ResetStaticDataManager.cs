using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetStaticDataManager : MonoBehaviour {


    private void Awake() {
        CuttingCounter.ResetStaticData();
        BaseCounter.ResetStaticData();
        TrashCounter.ResetStaticData();
        Player.ResetStaticData();
        CustomerTable.ResetStaticData();
        FuseBoxCounter.ResetStaticData();
        PipeWrenchCounter.ResetStaticData();
        PortablePipeWrench.ResetStaticData();
        FireExtinguisherCounter.ResetStaticData();
        PortableFireExtinguisher.ResetStaticData();
        DayLoopManager.ResetStaticData();
        NightMarketManager.ResetStaticData();
        SinkCounter.ResetStaticData();
        CoopPingManager.ResetStaticData();
        GameInput.ResetStaticData();
        DeepFryerCounter.ResetStaticData();
        BeverageDispenserCounter.ResetStaticData();
        DineInCustomerController.ResetStaticData();
    }



}
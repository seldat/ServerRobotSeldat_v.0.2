﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeldatMRMS;
using SeldatMRMS.Management;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using SelDatUnilever_Ver1._00.Communication.HttpBridge;
using SelDatUnilever_Ver1._00.Management.DeviceManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using static SelDatUnilever_Ver1.CollectionDataService;

namespace SelDatUnilever_Ver1._00.Management.UnityService
{
   public class TaskRounterService
    {
        public enum ProcessAssignAnTaskWait
        {
            PROC_ANY_IDLE = 0,
            PROC_ANY_CHECK_HAS_ANTASK,
            PROC_ANY_ASSIGN_ANTASK,
            PROC_ANY_GET_ANROBOT_IN_WAITTASKLIST,
            PROC_ANY_CHECK_ROBOT_BATTERYLEVEL,
            PROC_ANY_SET_TRAFFIC_RISKAREA_ON,
            PROC_ANY_CHECK_ROBOT_OUTSIDEREADY,
            PROC_ANY_CHECK_ROBOT_GOTO_READY
        }
        protected enum ProcessAssignTaskReady
        {
            PROC_READY_IDLE = 0,
            PROC_READY_CHECK_HAS_ANTASK,
            PROC_READY_ASSIGN_ANTASK,
            PROC_READY_GET_ANROBOT_INREADYLIST,
            PROC_READY_CHECK_ROBOT_BATTERYLEVEL,
            PROC_READY_SET_TRAFFIC_RISKAREA_ON,
            PROC_READY_CHECK_ROBOT_OUTSIDEREADY,
        }
        protected ProcessAssignTaskReady processAssignTaskReady;
        
        protected ProcessAssignAnTaskWait processAssignAnTaskWait;
        protected ProcedureManagementService procedureService;
        public RobotManagementService robotManageService;
        public TrafficManagementService trafficService;
        public List<DeviceItem> deviceItemsList;
        public bool Alive = false;
        public bool FlagAssign = true;
        public bool onFlagBusyGetTask = false;
        public void RegistryService(RobotManagementService robotManageService)
        {
            this.robotManageService = robotManageService;
        }
        public void RegistryService(TrafficManagementService trafficService)
        {
            this.trafficService = trafficService;
        }
        public void RegistryService(ProcedureManagementService procedureService)
        {
            this.procedureService = procedureService;
        }
        public void RegistryService(List<DeviceItem> deviceItemsList)
        {
            this.deviceItemsList = deviceItemsList;
        }
        public TaskRounterService() {
            //processAssignAnTaskState = ProcessAssignAnTask.PROC_IDLE;
        }
        public void MoveElementToEnd()
        {
            try
            {
                if (deviceItemsList.Count > 1)
                {
                    var element = deviceItemsList[0];
                    deviceItemsList.RemoveAt(0);
                    deviceItemsList.Add(element);
                }
            }
            catch { }
        }
        public OrderItem Gettask()
        {
            OrderItem item = null;
            int palletId = -1;
         //   if (!onFlagBusyGetTask)
            {
                onFlagBusyGetTask = true;
              
                if (deviceItemsList.Count > 0)
                {
                    try
                    {
                        item = deviceItemsList[0].GetOrder();
                        if (item == null)
                            return null;

                        switch (item.typeReq)
                        {
                            case TyeRequest.TYPEREQUEST_BUFFER_TO_MACHINE:
                            {
                                    palletId = GetPalletId(item.dataRequest);
                                    if (palletId > 0)
                                    {
                                        dynamic product = new JObject();
                                        UpdatePalletStateToHold(palletId, item);
                                        product.timeWorkId = item.timeWorkId;
                                        product.activeDate = item.activeDate;
                                        product.productId = item.productId;
                                        product.productDetailId = item.productDetailId;
                                        // chu y sua 
                                        product.palletStatus = PalletStatus.H.ToString(); // W
                                        item.dataRequest = product.ToString();
                                        item.palletId_H = palletId;
                                        return item;
                                    }
                                    else
                                    {
                                        return null;
                                    }
                            }
                            case TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFERRETURN_TO_BUFFER401:
                                {
                                    palletId = GetPalletId(item.dataRequest);
                                    if (palletId > 0)
                                    {
                                        dynamic productBR = new JObject();
                                        UpdatePalletStateToHold(palletId, item);
                                        productBR.timeWorkId = item.timeWorkId;
                                        productBR.activeDate = item.activeDate;
                                        productBR.productId = item.productId;
                                        productBR.productDetailId = item.productDetailId;
                                        productBR.palletStatus = PalletStatus.H.ToString(); // đã giữ pallet lấy H
                                        item.dataRequest_BufferReturn = productBR.ToString();
                                        item.palletId_H = palletId;
                                        dynamic productB401 = new JObject();
                                        UpdatePalletStateToHold(palletId, item);
                                        productB401.timeWorkId = item.timeWorkId;
                                        productB401.activeDate = item.activeDate;
                                        productB401.productId = item.productId;
                                        productB401.productDetailId = item.productDetailId;
                                        productB401.palletStatus = PalletStatus.P.ToString(); // đã có pallet lấy P
                                        item.dataRequest_Buffer401 = productB401.ToString();
                                        return item;
                                    }
                                    else
                                        return null;
                                }
                            case TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFER_TO_GATE:
                                {
                                    palletId = GetPalletId(item.dataRequest);
                                    if (palletId > 0)
                                    {
                                        dynamic product = new JObject();
                                        UpdatePalletStateToHold(palletId, item);
                                        product.timeWorkId = item.timeWorkId;
                                        product.activeDate = item.activeDate;
                                        product.productId = item.productId;
                                        product.productDetailId = item.productDetailId;
                                        // chu y sua 
                                        product.palletStatus = PalletStatus.H.ToString(); // W
                                        item.dataRequest = product.ToString();
                                        item.palletId_H = palletId;
                                        return item;
                                    }
                                    else
                                    {
                                        return null;
                                    }
                                }
                                return item;
                            case TyeRequest.TYPEREQUEST_MACHINE_TO_BUFFERRETURN:
                                palletId = GetPalletId(item.dataRequest);
                                if (palletId > 0)
                                {
                                    item.palletId_P = palletId;
                                    UpdatePalletStateToPlan(palletId, item);
                                  }

                                break;
                            case TyeRequest.TYPEREQUEST_PALLET_EMPTY_MACHINE_TO_RETURN:
                                palletId = GetPalletId_Return(item.dataRequest);
                                if (palletId > 0)
                                {
                                    item.palletId_F = palletId;
                                    dynamic product = new JObject();
                                    UpdatePalletStateToPlan(palletId, item);
                                    product.palletStatus = PalletStatus.P.ToString(); // đã có pallet lấy P
                                    item.dataRequest = product.ToString();
                                }
                                else
                                    return null;
                                break;
                            default:
                                return item;
                               

                        }
                    }
                    catch { }
                    
                }
                onFlagBusyGetTask = false;
            }
            return item;
            
        }

        public void UpdatePalletStateToHold(int palletId,OrderItem item)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            dynamic product = new JObject();
            product.palletId = palletId;
            product.planId = item.planId;
            product.palletStatus = PalletStatus.H.ToString();
            product.updUsrId = Global_Object.userLogin;
            String collectionData = RequestDataProcedure(product.ToString(), url);
            Console.WriteLine(collectionData);

        }
        public void UpdatePalletStateToPlan(int palletId, OrderItem item)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            dynamic product = new JObject();
            product.palletId = palletId;
            product.planId = item.planId;
            product.palletStatus = PalletStatus.P.ToString();
            product.updUsrId = Global_Object.userLogin;
            String collectionData = RequestDataProcedure(product.ToString(), url);
            Console.WriteLine(collectionData);
        }


        public int GetPalletId(String dataReq)
        {
            int palletId = -1;
            String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
            if (collectionData.Length > 0)
            {
                try
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int temp_planId = (int)result["planId"];
                      //  if (temp_planId ==order.planId)
                        {
                            var bufferResults = result["buffers"][0];
                            var palletInfo = bufferResults["pallets"][0];
                            palletId = (int)palletInfo["palletId"];
                            break;
                        }
                    }
                }
                catch { }

            }
            return palletId;
        }
        public int GetPalletId_Return(String dataReq)
        {
            int palletId = -1;
            String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "buffer/getListBufferReturn");
            if (collectionData.Length > 0)
            {
                try
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        if (result["pallets"].Count() > 0)
                        {
                                var palletInfo = result["pallets"][0];
                                palletId = (int)palletInfo["palletId"];
                                break;
                        }
                    }
                }
                catch { }

            }
            return palletId;
        }
        public String RequestDataProcedure(String dataReq, String url)
        {
            //String url = Global_Object.url+"plan/getListPlanPallet";
            BridgeClientRequest clientRequest = new BridgeClientRequest();
            // String url = "http://localhost:8080";
            var data = clientRequest.PostCallAPI(url, dataReq);
            return data.Result;
        }
    }
}

using Newtonsoft.Json.Linq;
using SeldatMRMS;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using SelDatUnilever_Ver1._00.Communication.HttpBridge;
using SelDatUnilever_Ver1._00.Management.DeviceManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using static SelDatUnilever_Ver1.CollectionDataService;

namespace SelDatUnilever_Ver1._00.Management.UnityService
{
    public class TaskRounterService
    {
        public class PalletINF
        {
            public int row;
            public int bay;
            public int bayId;
            public int palletId;
            public int ofBufferId;
        }
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

        public OrderItem CheckHastask()
        {
            OrderItem item = null;
            if (deviceItemsList.Count > 0)
            {
                try
                {
                    item = deviceItemsList[0].GetOrder();
                    if (item == null)
                        return null;
                }
                catch
                {

                }
            }
            return item;
        }
        public OrderItem Gettask()
        {
            OrderItem item = null;
            int palletId = -1;
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
                                    if (item.onAssiged == false)
                                    {
                                        PalletINF palletIFBM = GetPalletId(item.dataRequest);
                                        if (palletIFBM != null)
                                        {
                                            palletId = palletIFBM.palletId;
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
                                            item.palletBay = palletIFBM.bay;
                                            item.palletRow = palletIFBM.row;
                                            item.bufferId = palletIFBM.ofBufferId;
                                            item.bayId = palletIFBM.bayId;
                                            item.onAssiged = true;
                                            if (checkRobotSameBayId(item.bayId))
                                            {
                                                return null;
                                            }
                                            else
                                            {
                                                return item;
                                            }
                                        }
                                        else
                                        {
                                            item.status = StatusOrderResponseCode.ERROR_GET_PALLETID;
                                            deviceItemsList[0].RemoveOrder(item);
                                            return null;
                                        }
                                    }
                                    else
                                    {
                                        if (checkRobotSameBayId(item.bayId))
                                        {
                                            return null;
                                        }
                                        else
                                        {
                                            return item;
                                        }
                                    }
                            }
                            case TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFERRETURN_TO_BUFFER401:
                                {
                                    PalletINF palletIFBRB401 = GetPalletId(item.dataRequest);
                                    if (palletIFBRB401!=null)
                                    {
                                        palletId = palletIFBRB401.palletId;
                                        dynamic productBR = new JObject();
                                        UpdatePalletStateToHold(palletId, item);
                                        productBR.timeWorkId = item.timeWorkId;
                                        productBR.activeDate = item.activeDate;
                                        productBR.productId = item.productId;
                                        productBR.productDetailId = item.productDetailId;
                                        productBR.palletStatus = PalletStatus.H.ToString(); // đã giữ pallet lấy H
                                        item.dataRequest_BufferReturn = productBR.ToString();
                                        item.palletId_H = palletId;
                                        item.palletBay = palletIFBRB401.bay;
                                        item.palletRow = palletIFBRB401.row;
                                        item.bufferId = palletIFBRB401.ofBufferId;

                                        dynamic productB401 = new JObject();
                                        //UpdatePalletStateToHold(palletId, item);
                                        productB401.timeWorkId = item.timeWorkId;
                                        productB401.activeDate = item.activeDate;
                                        productB401.productId = item.productId;
                                        productB401.productDetailId = item.productDetailId;
                                        productB401.palletStatus = PalletStatus.P.ToString(); // đã có pallet lấy P
                                        item.dataRequest_Buffer401 = productB401.ToString();
                                        return item;
                                    }
                                    else
                                    {
                                        item.status = StatusOrderResponseCode.ERROR_GET_PALLETID;
                                        deviceItemsList[0].RemoveOrder(item);
                                        return null;
                                    }
                                }
                            case TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFER_TO_GATE:
                                {
                                    palletId = item.palletId;
                                    if (palletId > 0)
                                    {
                                        dynamic product = new JObject();
                                      //  UpdatePalletStateToReturn(palletId, item);
                                        product.timeWorkId = item.timeWorkId;
                                        product.activeDate = item.activeDate;
                                        product.productId = item.productId;
                                        product.productDetailId = item.productDetailId;
                                        // chu y sua 
                                        product.palletStatus = PalletStatus.R.ToString(); // W
                                        item.dataRequest = product.ToString();
                                        item.palletId_H = palletId;
                                        return item;
                                    }
                                    else
                                    {
                                        item.status = StatusOrderResponseCode.ERROR_GET_PALLETID;
                                        deviceItemsList[0].RemoveOrder(item);
                                        return null;
                                    }
                                }
                              case TyeRequest.TYPEREQUEST_MACHINE_TO_BUFFERRETURN:
                                {
                                    PalletINF palletIFMBR = GetPalletId(item.dataRequest);
                                    if (palletIFMBR !=null)
                                    {
                                        palletId = palletIFMBR.palletId;
                                        item.palletBay = palletIFMBR.bay;
                                        item.palletRow = palletIFMBR.row;
                                        item.palletId_P = palletId;
                                        item.bufferId = palletIFMBR.ofBufferId;
                                        UpdatePalletStateToPlan(palletId, item);
                                    }
                                    else
                                    {
                                        item.status = StatusOrderResponseCode.ERROR_GET_PALLETID;
                                        deviceItemsList[0].RemoveOrder(item);
                                        return null;
                                    }
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
                                {
                                    item.status = StatusOrderResponseCode.ERROR_GET_PALLETID;
                                    deviceItemsList[0].RemoveOrder(item);
                                    return null;
                                }
                                break;
                            case TyeRequest.TYPEREQUEST_FORLIFT_TO_BUFFER:
                                if (item.onAssiged == false)
                                {
                                    PalletINF palletINF = GetRowBayPalletPlaned(item.dataRequest, item.palletId_P);
                                    if (palletINF != null)
                                    {
                                        item.palletBay = palletINF.bay;
                                        item.palletRow = palletINF.row;
                                        item.bufferId = palletINF.ofBufferId;
                                        item.onAssiged = true;
                                        return item;
                                    }
                                    else
                                    {
                                        item.status = StatusOrderResponseCode.ERROR_GET_PALLETID;
                                        deviceItemsList[0].RemoveOrder(item);
                                        return null;
                                    }
                                }
                                else
                                {
                                    return item;
                                }
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
        public void UpdatePalletStateToReturn(int palletId, OrderItem item)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            dynamic product = new JObject();
            product.palletId = palletId;
            product.planId = item.planId;
            product.palletStatus = PalletStatus.R.ToString();
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

        public PalletINF GetRowBayPalletPlaned(String dataRequest,int palletId_P)
        {
            PalletINF palletINF = new PalletINF();
            try
            {
                String collectionData = RequestDataProcedure(dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                        {
                            int temp_planId = (int)result["planId"];
                            {
                                foreach (var buffer in result["buffers"])
                                {
                                    int bufferId = (int)buffer["bufferId"];
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        foreach (var palletInfo in buffer["pallets"])
                                        {
                                        int bay = (int)palletInfo["bay"];
                                        int row = (int)palletInfo["row"];
                                        int palletId = (int)palletInfo["palletId"];
                                            if (palletId == palletId_P)
                                            {
                                                palletINF.palletId = palletId;
                                                palletINF.bay = bay;
                                                palletINF.row = row;
                                                palletINF.ofBufferId = bufferId;
                                            
                                                return palletINF;
                                            }

                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                break;

                            }
                        }
                    }
            }
            catch
            {
                Console.WriteLine("Error Front Line");
            }
            return null;
        }

        public bool checkRobotSameBayId(int bayId)
        {
            foreach(RobotUnity robot in this.robotManageService.RobotUnityRegistedList.Values)
            {
                if (robot.bayId == bayId)
                    return true;
            }
            return false;
        }
        public int GetBayId_BM(OrderItem order)
        {
            int bayId = -1;
            try
            {
                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                        JArray results = JArray.Parse(collectionData);                  
                        var result = results[0];
                        foreach (var buffer in result["buffers"])
                        {
                            int bufferId = (int)buffer["bufferId"];
                            String bufferDataStr = (String)buffer["bufferData"];
                            JObject stuffBData = JObject.Parse(bufferDataStr);
                            bool canOpEdit = (bool)stuffBData["canOpEdit"];
                            if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy
                                continue;
                            if (buffer["pallets"].Count() > 0 && bufferId == order.bufferId)
                            {
                                foreach (var palletInfo in buffer["pallets"])
                                {
                                    int palletId = (int)palletInfo["palletId"];
                                    if (palletId == order.palletId_H)
                                    {
                                        JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                        bayId = (int)stuff["bayId"];
                                        break;
                                    }

                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                }
            }
            catch
            {
                Console.WriteLine("Error Front Line");
            }
            return bayId;
        }
        public PalletINF GetPalletId(String dataReq)
        {
            PalletINF palletINF = new PalletINF();
            int palletId = -1;
            String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
            if (collectionData.Length > 0)
            {
                try
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        foreach (var buffer in result["buffers"])
                        {
                            int bufferId = (int)buffer["bufferId"];
                            String bufferDataStr = (String)buffer["bufferData"];
                            JObject stuffBData = JObject.Parse(bufferDataStr);
                            bool canOpEdit = (bool)stuffBData["canOpEdit"];
                            if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy
                                continue;
                            var bufferResults = buffer;
                            var palletInfo = bufferResults["pallets"][0];
                            int bay = (int)palletInfo["bay"];
                            int row = (int)palletInfo["row"];
                            palletId = (int)palletInfo["palletId"];
                            JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                            int bayId = (int)stuff["bayId"];
                            palletINF.palletId = palletId;
                            palletINF.bayId = bayId;
                            palletINF.bay =bay;
                            palletINF.row = row;
                            palletINF.ofBufferId = bufferId;
                            return palletINF;
                        }
                    }
                }
                catch { }

            }
            return null;
        }
        public int GetPalletId_Return(String dataReq)
        {
            PalletINF palletInF = new PalletINF();
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

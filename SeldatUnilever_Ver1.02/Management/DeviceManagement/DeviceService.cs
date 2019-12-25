using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeldatMRMS;
using SelDatUnilever_Ver1._00.Communication.HttpBridge;
using System;
using System.Linq;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using static SelDatUnilever_Ver1.CollectionDataService;

namespace SeldatUnilever_Ver1._02.Management.DeviceManagement
{
    public class DeviceService
    {
        public class PlanDataRequest
        {
            public int timeWorkId ;
            public String activeDate;
            public int productId;
            public int productDetailId;
            public int updUsrId = Global_Object.userLogin;
            public int deviceId;
            public int palletAmount = 1;
            
        }
        public class UpdatePalletRequest
        {
            public int palletId;
            public String palletStatus;
            public int planId;
            public int updUsrId = Global_Object.userLogin;
        }
        public DeviceService() { }
        public String RequestDataProcedure_POST(String dataReq, String url)
        {
            //String url = Global_Object.url+"plan/getListPlanPallet";
            BridgeClientRequest clientRequest = new BridgeClientRequest();
            // String url = "http://localhost:8080";
            var data = clientRequest.PostCallAPI(url, dataReq);

            return data.Result;
        }
        public String RequestDataProcedure_GET(String url)
        {
            //String url = Global_Object.url+"plan/getListPlanPallet";
            BridgeClientRequest clientRequest = new BridgeClientRequest();
            // String url = "http://localhost:8080";
            var data = clientRequest.GetCallAPI(url);

            return data.Result;
        }
        public bool UpdatePalletState(UpdatePalletRequest updatePalletRequest)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            dynamic product = new JObject();
            product.palletId = updatePalletRequest.palletId;
            product.planId = updatePalletRequest.planId;
            product.palletStatus = updatePalletRequest.palletStatus;
          //  product.updUsrId = Global_Object.userLogin;
            String data = RequestDataProcedure_POST(product.ToString(), url);
            if(Convert.ToInt32(data)>0)
            {
                return true;
            }
            return false;
        }
        public bool UpdatePalletStatusReturnBufferToGate(OrderItem order)
        {
            try
            {
                    dynamic product = new JObject();
                    UpdatePalletRequest updatePalletRequest = new UpdatePalletRequest();
                    updatePalletRequest.palletId = order.palletId;
                    updatePalletRequest.palletStatus = PalletStatus.R.ToString();
                    updatePalletRequest.planId = order.planId;
                    UpdatePalletState(updatePalletRequest);
                    product.timeWorkId = order.timeWorkId;
                    product.activeDate = order.activeDate;
                    product.productId = order.productId;
                    product.productDetailId = order.productDetailId;
                    // chu y sua 
                    product.palletStatus = PalletStatus.R.ToString();
                    order.dataRequest = product.ToString();
                    return true;
            }
            catch
            { }
                return false;
        }
        public String CreatePlanBuffer(OrderItem order)
        {
            dynamic product = new JObject();
            product.timeWorkId = 1;
            product.activeDate = order.activeDate;
            product.productId = order.productId;
            product.productDetailId = order.productDetailId;
            product.updUsrId = Global_Object.userLogin;
            product.deviceId = order.deviceId;
            product.palletAmount = 1;
            String response = RequestDataProcedure_POST(product.ToString(), Global_Object.url + "plan/createPlanPallet");
            return response;
        }
        public bool UpdatePalletStatusToHoldBufferReturn_BRB401(UpdatePalletRequest updatePalletRequest )
        {
            try
            {
                if(UpdatePalletState(updatePalletRequest))
                {
                    return true;
                }
            }
            catch
            { }
            return false;
        }
        public int CreatePlanBuffer401(PlanDataRequest plan)
        {
            
            try
            {
                String product = JsonConvert.SerializeObject(plan);
                String response = RequestDataProcedure_POST(product, Global_Object.url + "plan/createPlanPallet");
                return Convert.ToInt32(response);
                
            }
            catch { }
            return -1;
        }

        protected void FreePlanedBuffer(String dataReq,int planId)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            int _palletId = GetPalletId(dataReq);
            if (_palletId > 0)
            {
                dynamic product = new JObject();
                product.palletId = _palletId;
                product.planId = planId;
                product.palletStatus = PalletStatus.F.ToString();
                product.updUsrId = Global_Object.userLogin;
                var data = RequestDataProcedure_POST(product.ToString(), url);

            }

        }

        protected void FreePlanedBuffer(OrderItem order)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            int _palletId = GetPalletId(order.dataRequest,order.planId);
            if (_palletId > 0)
            {
                dynamic product = new JObject();
                product.palletId = _palletId;
                product.planId = order.planId;
                product.palletStatus = PalletStatus.F.ToString();
                product.updUsrId = Global_Object.userLogin;
                var data = RequestDataProcedure_POST(product.ToString(), url);

            }

        }
        public void UpdatePalletState(int palletId, int planId, PalletStatus palletStatus)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            dynamic product = new JObject();
            product.palletId = palletId;
            product.planId = planId;
            product.palletStatus = palletStatus.ToString();
            product.updUsrId = Global_Object.userLogin;
            String collectionData = RequestDataProcedure_POST(product.ToString(), url);
            Console.WriteLine(collectionData);

        }
        protected DataPallet GetLineMachineInfo(int deviceId)
        {
            try
            {
                dynamic product = new JObject();
                product.deviceId = deviceId;
                Pose poseTemp = null;
                String collectionData = RequestDataProcedure_POST(product.ToString(), Global_Object.url + "/device/getListDevicePallet");


                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    var result = results[0];
                    String jsonDPst = (string)result["dataPallet"];
                    JObject stuffPallet = JObject.Parse(jsonDPst);
                    double xx = (double)stuffPallet["line"]["x"];
                    double yy = (double)stuffPallet["line"]["y"];
                    double angle = (double)stuffPallet["line"]["angle"];
                    int row = (int)stuffPallet["pallet"]["row"];
                    int bay = (int)stuffPallet["pallet"]["bay"];
                    int directMain = (int)stuffPallet["pallet"]["dir_main"];
                    int directSub = (int)stuffPallet["pallet"]["dir_sub"];
                    int directOut = (int)stuffPallet["pallet"]["dir_out"];
                    int line_ord = (int)stuffPallet["pallet"]["line_ord"];
                    return new DataPallet() { linePos = new Pose(xx, yy, angle), row = row, bay = bay, directMain = directMain, directSub = directSub, directOut = directOut, line_ord = line_ord };

                }
            }
            catch { Console.WriteLine("Error in DeviceIte at GetLineMachineInfo"); }
            return null;
        }
        protected int getDeviceId(String deviceName)
        {
            //http://localhost:8081/robot/rest/device/getListDevice
            int deviceId = -1;
            String collectionData = RequestDataProcedure_GET(Global_Object.url + "/device/getListDevice");
            if (collectionData.Length > 0)
            {
                JArray results = JArray.Parse(collectionData);
                foreach(var result in results)
                {
                    String dvn = (string)result["deviceName"];
                    if(deviceName.Equals(dvn))
                    {
                        deviceId= (int)result["deviceId"];
                    }
                }
            }
            return deviceId;
        }

        public Pose GetFrontLineBuffer(OrderItem order, bool onPlandId = false)
        {
            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure_POST(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    if (onPlandId)
                    {
                        foreach (var result in results)
                        {
                            int temp_planId = (int)result["planId"];
                            if (temp_planId == order.planId)
                            {
                                //var bufferResults = result["buffers"][0];
                                foreach (var buffer in result["buffers"])
                                {

                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        foreach (var palletInfo in buffer["pallets"])
                                        {
                                            int palletId = (int)palletInfo["palletId"];
                                            if (palletId == order.palletId_P)
                                            {
                                                JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                              
                                                double x = (double)stuff["line"]["x"];
                                                double y = (double)stuff["line"]["y"];
                                                double angle = (double)stuff["line"]["angle"];
                                                poseTemp = new Pose(x, y, angle);
                                             
                                                break;
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
                    else
                    {

                        var result = results[0];
                        //var bufferResults = result["buffers"][0];
                        foreach (var buffer in result["buffers"])
                        {
                            String bufferDataStr = (String)buffer["bufferData"];
                            JObject stuffBData = JObject.Parse(bufferDataStr);
                            bool canOpEdit = (bool)stuffBData["canOpEdit"];
                            if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy
                                continue;
                            if (buffer["pallets"].Count() > 0)
                            {
                                foreach (var palletInfo in buffer["pallets"])
                                {
                                    int palletId = (int)palletInfo["palletId"];
                                    if (palletId == order.palletId_H)
                                    {
                                        JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                       
                                        double x = (double)stuff["line"]["x"];
                                        double y = (double)stuff["line"]["y"];
                                        double angle = (double)stuff["line"]["angle"];
                                        poseTemp = new Pose(x, y, angle);
                                       
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
                //  Console.WriteLine(""+poseTemp.Position.ToString());
            }
            catch
            {
                Console.WriteLine("Error Front Line");
            }
            return poseTemp;
        }
        protected int GetPalletId(String dataReq,int planId=0)
        {
            int palletId = -1;
            try
            {
                String collectionData = RequestDataProcedure_POST(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    try
                    {
                        JArray results = JArray.Parse(collectionData);
                        foreach (var result in results)
                        {
                            if (planId > 0)
                            {
                                int temp_planId = (int)result["planId"];
                                if (temp_planId == planId)
                                {
                                    foreach (var buffer in result["buffers"])
                                    {
                                        //var bufferResults = result["buffers"][0];
                                        if (buffer["pallets"].Count() > 0)
                                        {
                                            var bufferResults = buffer;
                                            var palletInfo = bufferResults["pallets"][buffer["pallets"].Count() - 1];
                                            palletId = (int)palletInfo["palletId"];
                                            return palletId;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (var buffer in result["buffers"])
                                {
                                    //var bufferResults = result["buffers"][0];
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        var bufferResults = buffer;
                                        var palletInfo = bufferResults["pallets"][buffer["pallets"].Count() - 1];
                                        palletId = (int)palletInfo["palletId"];
                                        return palletId;
                                    }
                                }
                            }
                        }

                    }
                    catch { }

                }
            }
            catch
            {
                Console.WriteLine("Errror Get palletID");
            }
            return palletId;
        }

    }


}

using Newtonsoft.Json.Linq;
using SeldatMRMS;
using SelDatUnilever_Ver1._00.Communication.HttpBridge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using static SelDatUnilever_Ver1.CollectionDataService;

namespace SeldatUnilever_Ver1._02.Management.DeviceManagement
{
    public class DeviceService
    {
        public DeviceService() { }
        public String RequestDataProcedure(String dataReq, String url)
        {
            //String url = Global_Object.url+"plan/getListPlanPallet";
            BridgeClientRequest clientRequest = new BridgeClientRequest();
            // String url = "http://localhost:8080";
            var data = clientRequest.PostCallAPI(url, dataReq);

            return data.Result;
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
            String response = RequestDataProcedure(product.ToString(), Global_Object.url + "plan/createPlanPallet");
            return response;
        }

        protected void FreePlanedBuffer(OrderItem order)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            int _palletId = GetPalletId(order);
            if (_palletId > 0)
            {
                dynamic product = new JObject();
                product.palletId = _palletId;
                product.planId = order.planId;
                product.palletStatus = PalletStatus.F.ToString();
                product.updUsrId = Global_Object.userLogin;
                var data = RequestDataProcedure(product.ToString(), url);

            }

        }

        protected DataPallet GetLineMachineInfo(int deviceId)
        {
            try
            {
                dynamic product = new JObject();
                product.deviceId = deviceId;
                Pose poseTemp = null;
                String collectionData = RequestDataProcedure(product.ToString(), Global_Object.url + "/device/getListDevicePallet");


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
        protected int GetPalletId(OrderItem order)
        {
            int palletId = -1;
            try
            {
                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    try
                    {
                        JArray results = JArray.Parse(collectionData);
                        foreach (var result in results)
                        {

                            int temp_planId = (int)result["planId"];
                            if (temp_planId == order.planId)
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

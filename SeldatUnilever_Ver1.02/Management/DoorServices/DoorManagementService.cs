using DoorControllerService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeldatUnilever_Ver1._02.Management.DoorServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static DoorControllerService.DoorService;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;

namespace SeldatMRMS.Management.DoorServices
{
    public class DoorManagementService
    {
        public const Int32 AmountofDoor = 4;
        public ListCollectionView Grouped_PropertiesDoor { get; private set; }
        public List<DoorInfoConfig> PropertiesDoor_List;
        private List<DoorInfoConfig> DoorInfoConfigList;
        public DoorService DoorMezzamineUp;
        public DoorService DoorMezzamineUp_InV; // toa độ đầu line inverse
        public DoorService DoorMezzamineUpNew;
        public DoorService DoorMezzamineUpNew_InV;
        public DoorService DoorMezzamineReturn;
        public DoorService DoorMezzamineReturn_InV;
        public DoorElevator DoorElevator;
        public DoorConfigure doorConfigure;
        static public bool fistInit = false;
        public DoorManagementService(){
            // LoadDoorConfigure();
            DoorInfoConfigList = new List<DoorInfoConfig>();
            PropertiesDoor_List = new List<DoorInfoConfig>();
            Grouped_PropertiesDoor = (ListCollectionView)CollectionViewSource.GetDefaultView(PropertiesDoor_List);
            if (fistInit == false)
            {
                fistInit = true;
                LoadConfigure();

                DoorMezzamineUpNew = new DoorService(DoorInfoConfigList[0]);
                DoorMezzamineUpNew_InV = new DoorService(DoorInfoConfigList[1]);
                DoorMezzamineUp = new DoorService(DoorInfoConfigList[2]);
                DoorMezzamineUp_InV = new DoorService(DoorInfoConfigList[3]);
                DoorMezzamineReturn = new DoorService(DoorInfoConfigList[4]); // kiem tra lai 
                DoorMezzamineReturn_InV = new DoorService(DoorInfoConfigList[5]); // kiem tra lai 
                try
                {
                    doorConfigure = new DoorConfigure(this);
                }
                catch { }
            }
        }
    
        public void AddDoor()
        {
                PropertiesDoor_List.Add(new DoorInfoConfig());
                Grouped_PropertiesDoor.Refresh();
        }
        public void Initialize()
        {
            DoorInfoConfig doorICF_MUB_New = new DoorInfoConfig()
            {
                Name = "GATE1",
                Id = DoorId.DOOR_MEZZAMINE_UP_NEW,
                Ip = "192.168.1.242",
                Port = 8081,
                infoPallet = "{\"pallet\":0,\"dir_main\":1,\"dir_out\":0,\"bay\":1,\"hasSubLine\":\"no\",\"line_ord\":0,\"dir_sub\":0,\"row\":0}",
                PointCheckInGateStr = "2.54,-6.78,90",
                PointFrontLineStr = "15.1,0.7,0"
            };
            doorICF_MUB_New.ParsePointCheckInGateValue(doorICF_MUB_New.PointCheckInGateStr);
            doorICF_MUB_New.ParsePointFrontLineValue(doorICF_MUB_New.PointFrontLineStr);
            PropertiesDoor_List.Add(doorICF_MUB_New);
            DoorInfoConfigList.Add(doorICF_MUB_New);

            DoorInfoConfig doorICF_MUB_NEW_INV = new DoorInfoConfig()
            {
                Name = "GATE1-INV",
                Id = DoorId.DOOR_MEZZAMINE_UP_NEW,
                Ip = "192.168.1.242",
                Port = 8081,
                infoPallet = "{\"pallet\":0,\"dir_main\":2,\"dir_out\":0,\"bay\":1,\"hasSubLine\":\"no\",\"line_ord\":0,\"dir_sub\":0,\"row\":0}",
                PointCheckInGateStr = "2.54,-6.78,90",
                PointFrontLineStr = "13.1,0.7,180"
            };
            doorICF_MUB_NEW_INV.ParsePointCheckInGateValue(doorICF_MUB_NEW_INV.PointCheckInGateStr);
            doorICF_MUB_NEW_INV.ParsePointFrontLineValue(doorICF_MUB_NEW_INV.PointFrontLineStr);
            PropertiesDoor_List.Add(doorICF_MUB_NEW_INV);
            DoorInfoConfigList.Add(doorICF_MUB_NEW_INV);

            DoorInfoConfig doorICF_MUB = new DoorInfoConfig()
            {
                Name="GATE2",
                Id = DoorId.DOOR_MEZZAMINE_UP,
                Ip = "192.168.1.240",
                Port = 8081,
                infoPallet = "{\"pallet\":0,\"dir_main\":1,\"dir_out\":0,\"bay\":1,\"hasSubLine\":\"no\",\"line_ord\":0,\"dir_sub\":0,\"row\":0}",
                PointCheckInGateStr = "2.54,-6.78,90",
                PointFrontLineStr     = "17.88,0.7,0"
            };
            doorICF_MUB.ParsePointCheckInGateValue(doorICF_MUB.PointCheckInGateStr);
            doorICF_MUB.ParsePointFrontLineValue(doorICF_MUB.PointFrontLineStr);
            PropertiesDoor_List.Add(doorICF_MUB);
            DoorInfoConfigList.Add(doorICF_MUB);

            DoorInfoConfig doorICF_MUB_INV = new DoorInfoConfig()
            {
                Name = "GATE2-INV",
                Id = DoorId.DOOR_MEZZAMINE_UP,
                Ip = "192.168.1.240",
                Port = 8081,
                infoPallet = "{\"pallet\":0,\"dir_main\":2,\"dir_out\":0,\"bay\":1,\"hasSubLine\":\"no\",\"line_ord\":0,\"dir_sub\":0,\"row\":0}",
                PointCheckInGateStr = "2.54,-6.78,90",
                PointFrontLineStr = "16.5,0.7,180"
            };
            doorICF_MUB_INV.ParsePointCheckInGateValue(doorICF_MUB_INV.PointCheckInGateStr);
            doorICF_MUB_INV.ParsePointFrontLineValue(doorICF_MUB_INV.PointFrontLineStr);
            PropertiesDoor_List.Add(doorICF_MUB_INV);
            DoorInfoConfigList.Add(doorICF_MUB_INV);



            DoorInfoConfig doorICF_MRB = new DoorInfoConfig()
            {
                Name = "GATE_RETURN",
                Id = DoorId.DOOR_MEZZAMINE_RETURN,
                Ip = "192.168.1.241",
                Port = 8081,
                infoPallet = "{\"pallet\":1\"dir_main\":1,\"dir_out\":0,\"bay\":1,\"hasSubLine\":\"no\",\"line_ord\":0,\"dir_sub\":0,\"row\":0}",
                PointCheckInGateStr   = "2.54,-6.78,90",
                PointFrontLineStr = "39,0.7,0"
            };
            doorICF_MRB.ParsePointCheckInGateValue(doorICF_MRB.PointCheckInGateStr);
            doorICF_MRB.ParsePointFrontLineValue(doorICF_MRB.PointFrontLineStr);
            PropertiesDoor_List.Add(doorICF_MRB);
            DoorInfoConfigList.Add(doorICF_MRB);

            DoorInfoConfig doorICF_MRB_INV = new DoorInfoConfig()
            {
                Name = "GATE_RETURN_INV",
                Id = DoorId.DOOR_MEZZAMINE_RETURN,
                Ip = "192.168.1.241",
                Port = 8081,
                infoPallet = "{\"pallet\":1\"dir_main\":2,\"dir_out\":0,\"bay\":1,\"hasSubLine\":\"no\",\"line_ord\":0,\"dir_sub\":0,\"row\":0}",
                PointCheckInGateStr = "2.54,-6.78,90",
                PointFrontLineStr = "36,0.7,180"
            };
            doorICF_MRB_INV.ParsePointCheckInGateValue(doorICF_MRB_INV.PointCheckInGateStr);
            doorICF_MRB_INV.ParsePointFrontLineValue(doorICF_MRB_INV.PointFrontLineStr);
            PropertiesDoor_List.Add(doorICF_MRB_INV);
            DoorInfoConfigList.Add(doorICF_MRB_INV);

            Grouped_PropertiesDoor.Refresh();
        }
        public void SaveConfig(String data)
        {
            String path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ConfigDoor.json");
            System.IO.File.WriteAllText(path,data);
        }
        public bool LoadConfigure()
        {
            String path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ConfigDoor.json");
            if (!File.Exists(path))
            {
                Initialize();
                SaveConfig(JsonConvert.SerializeObject(PropertiesDoor_List, Formatting.Indented).ToString());
                return false;
            }
            else
            {
                try
                {
                    String data = File.ReadAllText(path);
                    if (data.Length > 0)
                    {

                     JArray results = JArray.Parse(data);
                        foreach (var result in results)
                        {
                            DoorInfoConfig doorICF = new DoorInfoConfig()
                            {
                                Id = (DoorId)((int)result["Id"]),
                                Name = (String)result["Name"],
                                Ip = (String)result["Ip"],
                                Port = (int)result["Port"],
                                infoPallet =(String)result["infoPallet"],
                                PointFrontLineStr = (String)result["PointFrontLineStr"],
                                PointCheckInGateStr = (String)result["PointCheckInGateStr"]
                            };
                            doorICF.ParsePointCheckInGateValue(doorICF.PointCheckInGateStr);
                            doorICF.ParsePointFrontLineValue(doorICF.PointFrontLineStr);
                            PropertiesDoor_List.Add(doorICF);
                            DoorInfoConfigList.Add(doorICF);
                       }
                        Grouped_PropertiesDoor.Refresh();
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }
       
        public void ResetAllDoors()
        {

        }
        public void DisposeAllDoors()
        {

        }
        public void FixedConfigure(DoorId id, DoorInfoConfig dcf)
        {
           /* if (ChargerStationList.ContainsKey(id))
            {
                ChargerStationList[(ChargerId)id].UpdateConfigure(chcf);
            }*/
        }
    }
}

using DoorControllerService;
using SeldatMRMS;
using SeldatMRMS.Management.DoorServices;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static DoorControllerService.DoorService;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatUnilever_Ver1._02.Management.ProcedureServices
{
    public class TrafficProcedureService: ProcedureControlServices
    {
        TrafficManagementService traffic;
        DoorManagementService doorservice;
        RobotUnity robot;
        protected double DISTANCE_CHECk_BAYID = 11; //meter 8
        public TrafficProcedureService(RobotUnity robot, DoorManagementService doorservice, TrafficManagementService trafficService) :base(robot)
        {
            this.traffic = trafficService;
            this.doorservice = doorservice;
            this.robot = robot;
        }
        public TrafficProcedureService(RobotUnity robot,TrafficManagementService trafficService) : base(robot)
        {
            this.traffic = trafficService;
            this.robot = robot;
        }
        public TrafficProcedureService(RobotUnity robot) : base(robot)
        {
            this.robot = robot;
        }
        public class ZoneRegCheckIn
        {
            List<Point> ZoneReg;
            List<Point> ZoneCheckIn;
        }
        protected bool _TrafficCheckInBuffer(Pose frontLinePoint, int bayId)
        {
            // khu vực check in

            List<Point> ListZonePo123 = new List<Point>();
            List<Point> ListAkas123 = new List<Point>();

            // tìm vị trí đầu line có nằm trong khu vực check in
            // nếu có trong check in kiểm tra đã tới vùng check in chưa // check in chia ra cho từng vủng
            // đã tới check in check tiếp vị trí robot khác trong vùng này đang làm việc

            // PO123  { -49,-9}  {-11,-9}  {-11,-23}  {-49,-23}
            // CHECKINPO123_JUJ   {-49,-6} {-39,-6} {-39,-13} {-49,-13}
            // CHECKINPO123_VLP   {-64,-9} {-49,-9} {-49,-18} {-64,-18}
            // AKAS123  { -11,-9}  {6,-9}  {6,-23}  {-11,-23}
            // CHECKINAKAS123 {-12,-6} {-5,-6} {-5,-13} {-12,-13}
            // JULEVO/CHECKINPO123_VLP   {-64,-9} {-49,-9} {-49,-18} {-64,-18}
            // CHECKINJULEVO {-64,-6} {-57,-6} {-57,-13} {-64,-13} 


            Point rPFrontLine = Global_Object.CoorCanvas(frontLinePoint.Position);
            Point rP= Global_Object.CoorCanvas(robot.properties.pose.Position);
            if (this.traffic.RobotIsInArea("PO123", rPFrontLine))
            {
                if(this.traffic.RobotIsInArea("CHECKINPO123_JUJ", rP) )
                {
                    if (this.traffic.HasOtherRobotUnityinArea("PO123", robot))
                    {
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_STOP, true);
                        return true;// tiep tuc check
                    }
                    else
                    {
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
                        return false; // ket thuc check
                    }

                }
                else if (this.traffic.RobotIsInArea("CHECKINPO123_VLP", rP))
                {
                    if (this.traffic.HasOtherRobotUnityinArea("PO123", robot) || this.traffic.HasOtherRobotUnityinArea("CHECKINPO123_JUJ", robot))
                    {
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_STOP, true);
                        return true;// tiep tuc check
                    }
                    else
                    {
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
                        return false; // ket thuc check
                    }
                }

            }
            else if(this.traffic.RobotIsInArea("AKAS123", rPFrontLine))
            {
                if (this.traffic.RobotIsInArea("CHECKINAKAS123", rP))
                {
                    if (this.traffic.HasOtherRobotUnityinArea("AKAS123", robot))
                    {
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_STOP, true);
                        return true;// tiep tuc check
                    }
                    else
                    {
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
                        return false; // ket thuc check
                    }

                }
            }
            else if (this.traffic.RobotIsInArea("JULEVO", rPFrontLine))
            {
                if (this.traffic.RobotIsInArea("CHECKINJULEVO", rP))
                {
                    if (this.traffic.HasOtherRobotUnityinArea("JULEVO", robot))
                    {
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_STOP, true);
                        return true;// tiep tuc check
                    }
                    else
                    {
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
                        return false; // ket thuc check
                    }

                }
            }

            robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
            return false;// tiep tuc check
        }
        protected bool TrafficCheckInBuffer(Pose frontLinePoint,int bayId)
        {

            if (ExtensionService.CalDistance(robot.properties.pose.Position, frontLinePoint.Position) < DISTANCE_CHECk_BAYID)
            {
               /* if (robot.bayId < 0)
                {
                    robot.bayId = bayId;
                }*/
                List<RobotUnity> rCompList = checkAllRobotsHasInsideBayIdNear(bayId, 4);
                if (rCompList == null) // đã có 1 robot đã đăng ký thành công bayid
                {
                    robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_STOP, true);
                    return true;// tiep tuc check
                }
                if (rCompList.Count>0)
                {
                    // so sanh vi tri robot voi robot con lai
                    if (checkRobotToFrontLineDistanceCtrl(robot, rCompList, frontLinePoint.Position))
                    {
                        robot.bayIdReg = true;
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
                        return false; // ket thuc check
                    }
                    else
                    {
                       
                        robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_STOP, true);
                        return true; // tiep tuc check
                    }

                }
                else
                {
                    robot.bayIdReg = true;
                    robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
                    return false;// tiep tuc check
                }
            }
            else
            {
                robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL,false);
                return false;// tiep tuc check
            }
        }
        protected bool checkAnyRobotAtElevator(RobotUnity robot)
        {
            foreach (RobotUnity ar in robotService.RobotUnityRegistedList.Values)
            {
                if(this.traffic.RobotIsInArea("ELEVATOR", ar.properties.pose.Position))
                {
                    robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_STOP, true);
                    return true;
                }
            }
            robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL,false);
            return false;
        }
        protected List<RobotUnity> checkAllRobotsHasInsideBayIdNear(int bayId,int numLine)
        {
                List<RobotUnity> robotList = new List<RobotUnity>();
                foreach (RobotUnity robot in robotService.RobotUnityRegistedList.Values)
                {
                    if (robot != this.robot)
                    {
                        if (robot.bayId >= 0)
                        {
                            if (Math.Abs(robot.bayId-bayId)<= numLine)
                            {
                                if (!robot.bayIdReg)
                                    robotList.Add(robot);
                                else
                                    return null;
                            }
                        }
                    }
                }
                return robotList;
        }
        protected bool checkRobotToFrontLineDistanceCtrl(RobotUnity rThis, List<RobotUnity> rCompList, Point frontLine)
        {
            double distRThisMin = ExtensionService.CalDistance(rThis.properties.pose.Position, frontLine);
            foreach (RobotUnity rComp in rCompList)
            {
                double distRComp = ExtensionService.CalDistance(rComp.properties.pose.Position, frontLine);
                if (distRThisMin > distRComp)
                {
                    return false;
                }
            }
            return  true;
        }
        public class DoorServiceCtrl
        {
            public DoorService doorService;
            public Pose PointFrontLine;
            public String infoPallet;
        }
        protected DoorServiceCtrl getDoorService()
        {
            DoorServiceCtrl doorServiceCtrl = new DoorServiceCtrl();
   
            if (this.traffic.RobotIsInArea("OUTER", robot.properties.pose.Position) || this.traffic.RobotIsInArea("READY", robot.properties.pose.Position,TypeZone.OPZS))
            {
                if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineUp;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineUp.config.PointFrontLine;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineUp.config.infoPallet;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP_NEW)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineUpNew;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineUpNew.config.PointFrontLine;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineUpNew.config.infoPallet;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_RETURN)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineReturn;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineReturn.config.PointFrontLine;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineReturn.config.infoPallet;
                }
            }
            else if (this.traffic.RobotIsInArea("VIM-BTLCAP", robot.properties.pose.Position))
            {
                if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineUp;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineUp.config.PointFrontLineInv;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineUp.config.infoPalletInv;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP_NEW)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineUpNew;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineUpNew.config.PointFrontLineInv;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineUpNew.config.infoPalletInv;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_RETURN)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineReturn;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineReturn.config.PointFrontLine;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineReturn.config.infoPallet;
                }
            }
            else if (this.traffic.RobotIsInArea("ELEVATOR", robot.properties.pose.Position)) // VIM1 khu cạnh elevator
            {
                if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineUp;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineUp.config.PointFrontLineInv;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineUp.config.infoPalletInv;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP_NEW)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineUpNew;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineUpNew.config.PointFrontLineInv;
                    doorServiceCtrl.infoPallet= this.doorservice.DoorMezzamineUpNew.config.infoPalletInv;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_RETURN)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineReturn;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineReturn.config.PointFrontLine;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineReturn.config.infoPallet;
                }
            }
            else if (this.traffic.RobotIsInArea("GATE3", robot.properties.pose.Position)) // VIM2 khu cạnh gate3
            {
                if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineUp;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineUp.config.PointFrontLineInv;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineUp.config.infoPalletInv;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP_NEW)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineUpNew;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineUpNew.config.PointFrontLineInv;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineUpNew.config.infoPalletInv;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_RETURN)
                {
                    doorServiceCtrl.doorService = this.doorservice.DoorMezzamineReturn;
                    doorServiceCtrl.PointFrontLine = this.doorservice.DoorMezzamineReturn.config.PointFrontLineInv;
                    doorServiceCtrl.infoPallet = this.doorservice.DoorMezzamineReturn.config.infoPalletInv;
                }
            }
            return doorServiceCtrl;
        }

    }
}

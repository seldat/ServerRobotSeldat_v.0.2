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
        protected double DISTANCE_CHECk_BAYID = 4.5; //meter 8
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
        protected bool TrafficCheckInBuffer(Pose frontLinePoint,int bayId)
        {

            if (ExtensionService.CalDistance(robot.properties.pose.Position, frontLinePoint.Position) < DISTANCE_CHECk_BAYID)
            {
                if (robot.bayId < 0)
                {
                    robot.bayId = bayId;
                }
                List<RobotUnity> rCompList = checkAllRobotsHasInsideBayIdNear(bayId, 2);
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
                    
                    robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
                    return true;// tiep tuc check
                }
            }
            else
            {
                robot.SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL,false);
                return true;// tiep tuc check
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
        protected List<RobotUnity> checkAllRobotsHasInsideBayIdNear(int bayId,int step)
        {
                List<RobotUnity> robotList = new List<RobotUnity>();
                foreach (RobotUnity robot in robotService.RobotUnityRegistedList.Values)
                {
                    if (robot != this.robot)
                    {
                        if (robot.bayId > 0)
                        {
                            if (Math.Abs(robot.bayId-bayId)<=3)
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
        protected DoorService getDoorService()
        {
            DoorService door=null;     
            if (this.traffic.RobotIsInArea("OUTER", robot.properties.pose.Position) || this.traffic.RobotIsInArea("READY", robot.properties.pose.Position,TypeZone.OPZS))
            {
                if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP)
                {
                    door = this.doorservice.DoorMezzamineUp;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP_NEW)
                {
                    door = this.doorservice.DoorMezzamineUpNew;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_RETURN)
                {
                    door = this.doorservice.DoorMezzamineReturn;
                }
            }
            else if (this.traffic.RobotIsInArea("VIM-BTLCAP", robot.properties.pose.Position))
            {
                if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP)
                {
                    door = this.doorservice.DoorMezzamineUp_InV;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP_NEW)
                {
                    door = this.doorservice.DoorMezzamineUpNew_InV;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_RETURN)
                {
                    door = this.doorservice.DoorMezzamineReturn;
                }
            }
            else if (this.traffic.RobotIsInArea("ELEVATOR", robot.properties.pose.Position)) // VIM1 khu cạnh elevator
            {
                if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP)
                {
                    door = this.doorservice.DoorMezzamineUpNew_InV;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP_NEW)
                {
                    door = this.doorservice.DoorMezzamineUpNew_InV;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_RETURN)
                {
                    door = this.doorservice.DoorMezzamineReturn;
                }
            }
            else if (this.traffic.RobotIsInArea("GATE3", robot.properties.pose.Position)) // VIM2 khu cạnh gate3
            {
                if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP)
                {
                    door = this.doorservice.DoorMezzamineUpNew_InV;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP_NEW)
                {
                    door = this.doorservice.DoorMezzamineUpNew_InV;
                }
                else if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_RETURN)
                {
                    door = this.doorservice.DoorMezzamineReturn_InV;
                }
            }
            return door;
        }

    }
}

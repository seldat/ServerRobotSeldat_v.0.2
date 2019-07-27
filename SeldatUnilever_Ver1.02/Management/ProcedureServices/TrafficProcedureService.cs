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
        protected void TrafficCheckInBuffer(Pose frontLinePoint,int bayId)
        {
            if (ExtensionService.CalDistance(robot.properties.pose.Position,frontLinePoint.Position)< 120)
            {
                if(robot.bayId<0)
                {
                    robot.bayId = bayId;
                }
                if(checkAllRobotsHasInsideBayIdNear(bayId, 2))
                {
                    robot.SetSpeed(RobotSpeedLevel.ROBOT_SPEED_STOP);
                }
                else
                {
                    robot.SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                }
            }
            else
            {
                robot.SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
            }
            // kiem tra khoan cach robot hen hanh den diem dau line
            // neu gan diem dau line 
            // check co robot nao lam viec trong vung bayId do khong. neu co ngung lai
            // check co robot nao lam viec o line do ma co robot lam o nhung line lan can bayid co ngung lai
            //
        }
        protected bool checkAllRobotsHasInsideBayIdNear(int bayId,int step)
        {
            for (int cnt = 0; cnt <= step; cnt++)
            {
                foreach (RobotUnity robot in robotService.RobotUnityRegistedList.Values)
                {
                    if (robot != this.robot)
                    {
                        if (robot.bayId > 0)
                        {
                            if (robot.bayId == (bayId + cnt) || robot.bayId == (bayId - cnt))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
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
            if (this.traffic.RobotIsInArea("VIM", robot.properties.pose.Position))
            {
                if ((DoorId)order.gate == DoorId.DOOR_MEZZAMINE_UP)
                {
                    door = this.doorservice.DoorMezzamineUp;
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
            if (this.traffic.RobotIsInArea("ELEVATOR", robot.properties.pose.Position)) // VIM1 khu cạnh elevator
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
            if (this.traffic.RobotIsInArea("GATE3", robot.properties.pose.Position)) // VIM2 khu cạnh gate3
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

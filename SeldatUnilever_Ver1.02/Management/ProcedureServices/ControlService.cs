using SeldatMRMS.Management.RobotManagent;
using System;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;

namespace SeldatMRMS
{
    public class ControlService:DBProcedureService
    {
        RobotUnity robot;
       public ControlService(RobotUnity robot):base(robot)
       {
            this.robot = robot;
            if (robot != null)
            {
                robot.FinishStatesCallBack += FinishStatesCallBack;
                robot.LineEnableCallBack += LineEnableCallBack;
            }
       }
       // robot control
       public virtual void ZoneHandler(Communication.Message message) { }
       public virtual void FinishStatesCallBack(Int32 message) {

            ResponseCommand resCmd = (ResponseCommand)message;
            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
            {
                robot.SwitchToDetectLine(false);
            }
        }
        public virtual void LineEnableCallBack(Int32 message)
        {
            ResponseCommand resCmd = (ResponseCommand)message;
            if (resCmd== ResponseCommand.RESPONSE_START_DETECT_LINE && !robot.onFlagGoBackReady)
            {
                robot.SwitchToDetectLine(true);
            }
        }
        public virtual void AmclPoseHandler(Communication.Message message) { }
       public virtual void CtrlRobotSpeed() { }
       public virtual void MoveBaseGoal() { }
       public virtual void AcceptDoSomething() { }
        // door control
       public virtual void ReceiveRounterEvent(String message) { }
        public virtual void KillEvent() {
            if (robot != null)
            {
                robot.FinishStatesCallBack -= FinishStatesCallBack;
                robot.LineEnableCallBack -= LineEnableCallBack;
            }
        }
    }
}

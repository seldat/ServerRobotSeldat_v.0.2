using System;
using System.Threading.Tasks;
using SeldatMRMS.Management.RobotManagent;
using SeldatUnilever_Ver1._02.Management.ProcedureServices;
using static SeldatMRMS.DBProcedureService;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnity;
using static SeldatMRMS.ProcedureControlServices;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;

namespace SeldatMRMS
{
    public class ProcedureManagementService : RegisterProcedureService {

        public ProcedureManagementService () { }

        public void Register (ProcedureItemSelected ProcedureItem, RobotUnity robot, OrderItem orderItem) {

            CleanUp();
            switch (ProcedureItem) {
                case ProcedureItemSelected.PROCEDURE_FORLIFT_TO_BUFFER:
                    ProcedureForkLiftToBuffer procfb = new ProcedureForkLiftToBuffer (robot, doorService, trafficService);
                    procfb.Registry(deviceService);
                    ProcedureDataItems profbDataItems = new ProcedureDataItems ();
                    profbDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocfb = new RegisterProcedureItem () { item = procfb, robot = robot, procedureDataItems = profbDataItems };
                    procfb.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procfb.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                    //RegisterProcedureItemList.Add (itemprocfb);
                    procfb.AssignAnOrder (orderItem);
                    robot.proRegistryInRobot.pFB = procfb;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_FORKLIFT_TO_BUFFER;
                    procfb.Registry(robotManagementService);
                    procfb.Start ();
                    break;
                case ProcedureItemSelected.PROCEDURE_BUFFER_TO_MACHINE:
                    ProcedureBufferToMachine procbm = new ProcedureBufferToMachine (robot, trafficService);
                    procbm.Registry(deviceService);
                    ProcedureDataItems prcobmDataItems = new ProcedureDataItems ();
                    prcobmDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocbm = new RegisterProcedureItem () { item = procbm, robot = robot, procedureDataItems = prcobmDataItems };
                    procbm.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procbm.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                    //RegisterProcedureItemList.Add (itemprocbm);
                    procbm.AssignAnOrder (orderItem);
                    robot.proRegistryInRobot.pBM = procbm;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_BUFFER_TO_MACHINE;
                    procbm.Registry(robotManagementService);
                    procbm.Start ();
                    break;
                case ProcedureItemSelected.PROCEDURE_BUFFER_TO_RETURN:
                    ProcedureBufferToReturn procbr = new ProcedureBufferToReturn (robot, trafficService);
                    ProcedureDataItems prcobrDataItems = new ProcedureDataItems ();
                    prcobrDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocbr = new RegisterProcedureItem () { item = procbr, robot = robot, procedureDataItems = prcobrDataItems };
                    procbr.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procbr.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                   // RegisterProcedureItemList.Add (itemprocbr);
                    procbr.AssignAnOrder (orderItem);
                    robot.proRegistryInRobot.pBR = procbr;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_BUFFER_TO_RETURN;
                    procbr.Registry(robotManagementService);
                    procbr.Start ();
                    break;
                case ProcedureItemSelected.PROCEDURE_PALLETEMPTY_MACHINE_TO_RETURN:
                    ProcedureMachineToReturn procmr = new ProcedureMachineToReturn (robot, trafficService);
                    ProcedureDataItems prcomrDataItems = new ProcedureDataItems ();
                    prcomrDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocmr = new RegisterProcedureItem () { item = procmr, robot = robot, procedureDataItems = prcomrDataItems };
                    procmr.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procmr.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                    //RegisterProcedureItemList.Add (itemprocmr);
                    procmr.AssignAnOrder (orderItem);
                    robot.proRegistryInRobot.pMR = procmr;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_MACHINE_TO_RETURN;
                    procmr.Registry(robotManagementService);
                    procmr.Start ();
                    break;
                case ProcedureItemSelected.PROCEDURE_RETURN_TO_GATE:
                    ProcedureReturnToGate procrg = new ProcedureReturnToGate (robot, doorService, trafficService);
                    ProcedureDataItems prorgDataItems = new ProcedureDataItems ();
                    prorgDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocrg = new RegisterProcedureItem () { item = procrg, robot = robot, procedureDataItems = prorgDataItems };
                    procrg.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procrg.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                   // RegisterProcedureItemList.Add (itemprocrg);
                    procrg.AssignAnOrder (orderItem);
                    procrg.Registry(robotManagementService);
                    procrg.Start ();
                    break;
                case ProcedureItemSelected.PROCEDURE_ROBOT_TO_CHARGE:
                    ProcedureRobotToCharger procrc = new ProcedureRobotToCharger (robot, chargerService, robot.properties.ChargeID);
                    ProcedureDataItems procrcDataItems = new ProcedureDataItems ();
                    procrcDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocrc = new RegisterProcedureItem () { item = procrc, robot = robot, procedureDataItems = procrcDataItems };
                    procrc.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procrc.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                  //  RegisterProcedureItemList.Add (itemprocrc);
                    robot.proRegistryInRobot.pRC = procrc;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_CHARGE;
                    procrc.Start ();
                    break;
                case ProcedureItemSelected.PROCEDURE_ROBOT_TO_READY:
                    ProcedureRobotToReady procrr = new ProcedureRobotToReady (robot, robot.properties.ChargeID, trafficService,chargerService, doorService.DoorMezzamineUp.config.PointCheckInGate);
                    ProcedureDataItems procrrDataItems = new ProcedureDataItems ();
                    procrrDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocrr = new RegisterProcedureItem () { item = procrr, robot = robot, procedureDataItems = procrrDataItems };
                    procrr.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procrr.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                   // RegisterProcedureItemList.Add (itemprocrr);
                    robot.proRegistryInRobot.pRR = procrr;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_READY;
                    procrr.Registry(deviceService);
                    procrr.Registry(robotManagementService);
                    procrr.Start ();
                    break;
                case ProcedureItemSelected.PROCEDURE_ROBOT_READY_TO_READY:
                    ProcedureRobotToReady procRrr = new ProcedureRobotToReady(robot, robot.properties.ChargeID, trafficService, chargerService,doorService.DoorMezzamineUp.config.PointCheckInGate);
                    ProcedureDataItems procRrrDataItems = new ProcedureDataItems();
                    procRrrDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocRrr = new RegisterProcedureItem() { item = procRrr, robot = robot, procedureDataItems = procRrrDataItems };
                    procRrr.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procRrr.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                   // RegisterProcedureItemList.Add(itemprocRrr);
                    robot.proRegistryInRobot.pRR = procRrr;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_READY;
                    procRrr.Registry(robotManagementService);
                    procRrr.Start();
                    break;
                case ProcedureItemSelected.PROCEDURE_FORLIFT_TO_MACHINE:
                    ProcedureForkLiftToMachine procfm = new ProcedureForkLiftToMachine(robot, doorService, trafficService);
                    ProcedureDataItems profmDataItems = new ProcedureDataItems();
                    profmDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocfm = new RegisterProcedureItem() { item = procfm, robot = robot, procedureDataItems = profmDataItems };
                    procfm.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procfm.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                    //RegisterProcedureItemList.Add(itemprocfm);
                    procfm.AssignAnOrder(orderItem);
                    robot.proRegistryInRobot.pFM = procfm;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_FORKLIFT_TO_MACHINE;
                    procfm.Registry(robotManagementService);
                    procfm.Start();
                    break;
                case ProcedureItemSelected.PROCEDURE_BUFFER_TO_BUFFER:
                    ProcedureBufferReturnToBuffer401 procbb = new ProcedureBufferReturnToBuffer401(robot, trafficService);
                    ProcedureDataItems prcobbDataItems = new ProcedureDataItems();
                    prcobbDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocbb = new RegisterProcedureItem() { item = procbb, robot = robot, procedureDataItems = prcobbDataItems };
                    procbb.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procbb.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                    // RegisterProcedureItemList.Add (itemprocbr);
                    procbb.AssignAnOrder(orderItem);
                    robot.proRegistryInRobot.pBB = procbb;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_BUFFER_TO_BUFFER;
                    procbb.Registry(robotManagementService);
                    procbb.Start();
                    break;
                case ProcedureItemSelected.PROCEDURE_BUFFER_TO_GATE:
                    ProcedureBufferToGate procbg = new ProcedureBufferToGate(robot, doorService, trafficService);
                    ProcedureDataItems probgDataItems = new ProcedureDataItems();
                    probgDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocbg = new RegisterProcedureItem() { item = procbg, robot = robot, procedureDataItems = probgDataItems };
                    procbg.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procbg.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                    // RegisterProcedureItemList.Add (itemprocrg);
                    procbg.AssignAnOrder(orderItem);
                    robot.proRegistryInRobot.pBG= procbg;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_BUFFER_TO_GATE;
                    procbg.Registry(robotManagementService);
                    procbg.Start();
                    break;
                case ProcedureItemSelected.PROCEDURE_MACHINE_TO_GATE:
                    ProcedureMachineToGate procmg = new ProcedureMachineToGate(robot, doorService, trafficService);
                    ProcedureDataItems promgDataItems = new ProcedureDataItems();
                    promgDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocmg = new RegisterProcedureItem() { item = procmg, robot = robot, procedureDataItems = promgDataItems };
                    procmg.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procmg.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                    // RegisterProcedureItemList.Add (itemprocrg);
                    procmg.AssignAnOrder(orderItem);
                    robot.proRegistryInRobot.pMG = procmg;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_MACHINE_TO_GATE;
                    procmg.Start();
                    break;
                case ProcedureItemSelected.PROCEDURE_MACHINE_TO_BUFFER_RETURN:
                    ProcedureMachineToBufferReturn procmbr = new ProcedureMachineToBufferReturn(robot, trafficService);
                    ProcedureDataItems prombrDataItems = new ProcedureDataItems();
                    prombrDataItems.StartTaskTime = DateTime.Now;
                    RegisterProcedureItem itemprocmbr = new RegisterProcedureItem() { item = procmbr, robot = robot, procedureDataItems = prombrDataItems };
                    procmbr.ReleaseProcedureHandler += ReleaseProcedureItemHandler;
                    procmbr.ErrorProcedureHandler += ErrorApprearInProcedureItem;
                    // RegisterProcedureItemList.Add (itemprocrg);
                    procmbr.AssignAnOrder(orderItem);
                    robot.proRegistryInRobot.pMBR = procmbr;
                    robot.ProcedureRobotAssigned = ProcedureControlAssign.PRO_MACHINE_TO_BUFFER_RETURN;
                    procmbr.Registry(robotManagementService);
                    procmbr.Start();
                    break;
            }
        }

        public void DisposeProcedure()
        {

        }
        protected void CleanUp()
        {
            /*if (RegisterProcedureItemList.Count > 0)
            {
                int index = 0;
                do
                {
                    RegisterProcedureItem procR = RegisterProcedureItemList[index];
                    if (procR.item.procedureStatus == ProcedureStatus.PROC_KILLED)
                    {
                        RegisterProcedureItemList.Remove(procR);
                    }
                    index++;

                } while (RegisterProcedureItemList.Count < index && RegisterProcedureItemList.Count >0);
            }*/
        }
        protected override void ReleaseProcedureItemHandler (Object item) {
            Task.Run (() => {
                ProcedureControlServices procItem = item as ProcedureControlServices;
                RobotUnity robot = procItem.GetRobotUnity ();
                Console.WriteLine("Procedure :" + procItem.procedureCode);
                Console.WriteLine("Robot Rleased :" + robot.properties.Label);
                Console.WriteLine(">>>>>>>>>");
                robot.border.Dispatcher.BeginInvoke (System.Windows.Threading.DispatcherPriority.Normal,
                    new Action (delegate () {
                        robot.setColorRobotStatus (RobotStatusColorCode.ROBOT_STATUS_OK);
                    }));
                if (procItem.procedureCode == ProcedureControlServices.ProcedureCode.PROC_CODE_ROBOT_TO_READY) {
                    
                    robotManagementService.AddRobotUnityReadyList (robot);

                } else if (procItem.procedureCode == ProcedureControlServices.ProcedureCode.PROC_CODE_ROBOT_TO_CHARGE) {

                    robotManagementService.AddRobotUnityReadyList (robot);
                } else {

                    robotManagementService.AddRobotUnityWaitTaskList (robot);
                    try
                    {
                        procItem.ReleaseProcedureHandler -= ReleaseProcedureItemHandler;
                    }
                    catch { }
                }

            /*    var element = RegisterProcedureItemList.Find (e => e.item.procedureCode == procItem.procedureCode);
                element.procedureDataItems.EndTime = DateTime.Now;
                element.procedureDataItems.StatusProcedureDelivered = "OK";
                RegisterProcedureItemList.Remove (element);*/
                
            });
        }

        protected override void ErrorApprearInProcedureItem (Object item) {

            // chờ xử lý // error staus is true;
            // báo sự cố cho lớp robotmanagement // đợi cho chờ xử lý// hủy bỏ quy trình 
            // add order lại list device
            ProcedureControlServices procItem = item as ProcedureControlServices;
            if (procItem.procedureCode == ProcedureCode.PROC_CODE_ROBOT_TO_READY) {

            } else if (procItem.procedureCode == ProcedureCode.PROC_CODE_ROBOT_TO_CHARGE) {

            } else {
                // lưu lại giá trị order
               // RestoreOrderItem (procItem.order);
            }
           
            //SolvedProblem pSP = new SolvedProblem(item);
            //pSP.Show();

          

            //robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_ERROR);
            // robot.border.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
            //                        new Action(delegate ()
            //                        {
            //                            robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_ERROR);
            //                        }));
            // SolvedProblem pSP = new SolvedProblem(item);
            // pSP.Show();

        }

    }
}

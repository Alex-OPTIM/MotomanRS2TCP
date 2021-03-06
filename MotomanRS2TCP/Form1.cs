﻿using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace MotomanRS2TCP
{
    public partial class Form1 : Form
    {
        private delegate void SafeCallDelegate(string text);
        private delegate void Label9Delegate(object sender, EventArgs e);
        private delegate void Label5Delegate(CRobPosVar posVar);
        private delegate void Label4Delegate();
        private delegate void Label6Delegate();
        private NodeSocketIO ioClient;
        private MotomanConnection xrc;
        private int uiUpdateCounter = 0;
        private decimal speedSP;
        private bool isCycling = false;
        private readonly short varIndex = 0;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;

            speedSP = 200;
            numericUpDown1.Value = speedSP;
            portNumber.Value = 3;


            ioClient = new NodeSocketIO();
            WriteLine("Starting Socket IO");
            ioClient.Connect();

            btnConnect.Enabled = false;
            StartApp();
            button7.Enabled = true;
        }


        private async void Form1_FormClosing(object sender, EventArgs e)
        {

            if (ioClient != null)
            {
                Console.WriteLine("Stopping Socket IO");
                await ioClient.Disconnect();
            }
            if (xrc != null)
            {
                await xrc.Disconnect();
                xrc = null;
                Console.WriteLine("Stopping XRC connection");
            }
        }


        private void StartApp()
        {
            try
            {
                
                xrc = new MotomanConnection((short)portNumber.Value);
                ioClient.SetXrc(xrc);

                xrc.StatusChanged += new EventHandler(StatusChanged);
                xrc.ConnectionStatus += new EventHandler(rc1_connectionStatus);
                xrc.EventStatus += new EventHandler(rc1_eventStatus);
                xrc.ConnectionError += new EventHandler(rc1_errorStatus);
                xrc.DispatchCurrentPosition += new EventHandler(
                    (object sender, EventArgs e) => { UpdateUiCurrentPosition(); }
                );
                xrc.MovingToPosition += new EventHandler(
                    (object sender, EventArgs e) => { UpdateUiSetpointPosition(); }
                );

                WriteLine("XRC Starting connection");
                xrc.Connect();



                UpdateUiSetpointPosition();
            } catch (Exception ex)
            {
                MessageBox.Show("StartApp(): " + ex.Message);
            }
        }

        private void rc1_connectionStatus(object sender, EventArgs e)
        {
            if (xrc == null) return;
            WriteLine("    XRC Connection: " + xrc.CurrentConnection);
        }

        private void rc1_eventStatus(object sender, EventArgs e)
        {
            if (xrc == null) return;
            if (label9.InvokeRequired)
            {
                label9.Invoke(new Label9Delegate(rc1_eventStatus), new object[] { sender, e });
            }
            else
            {
                label9.Text = xrc.CurrentEvent;
            }
        }

        private void rc1_errorStatus(object sender, EventArgs e)
        {
            if (xrc == null) return;
            WriteLine("    XRC Error: " + xrc.CurrentError);
        }

        private void StatusChanged(object sender, EventArgs e)
        {
            if (xrc == null) return;
            
            WriteLine("    XRC Status: " + xrc.RobotStatusJson);

            CRobotStatus status = xrc.GetCopyOfRobotStatus();
            if (!status.isTeach && status.isServoOn)
            {
                btnUp.Enabled = true;
                btnDown.Enabled = true;
                btnHomePos.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
                button6.Enabled = true;
                button1.Enabled = true;
                button8.Enabled = true;
            }
            else
            {
                btnUp.Enabled = false;
                btnDown.Enabled = false;
                btnHomePos.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
                button1.Enabled = false;
                button8.Enabled = false;
            }
        }

        public void WriteLine(string message)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new SafeCallDelegate(WriteLine), new object[] { message });
            } else
            {
                listBox1.Items.Add(message);
                if (listBox1.Items.Count > 100)
                {
                    listBox1.Items.RemoveAt(0);
                }

                // Make sure the last item is made visible
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                listBox1.ClearSelected();

            }
        }

        private void btnHomePos_Click(object sender, EventArgs e)
        {
            xrc.MoveToHome1();
        }

        //private void UpdateUiPositionVariable(CRobPosVar posVar)
        //{
        //    uiUpdateCounter++;
        //    if (label5.InvokeRequired)
        //    {
        //        label5.Invoke(new Label5Delegate(UpdateUiPositionVariable), new object[] { posVar });
        //    }
        //    else
        //    {
        //        label7.Text = uiUpdateCounter.ToString();
        //        if (posVar != null)
        //        {
        //            if (posVar.DataType == PosVarType.XYZ)
        //            {
        //                label5.Text = "X:" + posVar.X.ToString() + " " +
        //                    "Y:" + posVar.Y.ToString() + " " +
        //                    "Z:" + posVar.Z.ToString() + " " +
        //                    "Rx:" + posVar.Rx.ToString() + " " +
        //                    "Ry:" + posVar.Ry.ToString() + " " +
        //                    "Rz:" + posVar.Rz.ToString() + " " +
        //                    "F:" + posVar.Formcode.ToString() + " " +
        //                    "Tool:" + posVar.ToolNo.ToString();
        //            }
        //            else label5.Text = "Not XYZ coordinates";
        //        }
        //    }
        //}


        private void UpdateUiCurrentPosition()
        {
            if (xrc == null) return;

            if (label6.InvokeRequired)
            {
                label6.Invoke(new Label6Delegate(UpdateUiCurrentPosition));
            }
            else
            {
                var currentPosition = xrc.GetCurrentPositionCached();
                label6.Text = "X:" + currentPosition[0].ToString() + " " +
                    "Y:" + currentPosition[1].ToString() + " " +
                    "Z:" + currentPosition[2].ToString() + " " +
                    "Rx:" + currentPosition[3].ToString() + " " +
                    "Ry:" + currentPosition[4].ToString() + " " +
                    "Rz:" + currentPosition[5].ToString() + " " +
                    "F:" + currentPosition[13].ToString() + " " +
                    "Tool:" + currentPosition[14].ToString();
            }
        }

        private void UpdateUiSetpointPosition()
        {
            if (label4.InvokeRequired)
            {
                label4.Invoke(new Label4Delegate(UpdateUiSetpointPosition));
            }
            else
            {
                double[] movingTo = xrc.movingTo;
                label4.Text =
                    "X:" + movingTo[0] + " " +
                    "Y:" + movingTo[1] + " " +
                    "Z:" + movingTo[2] + " " +
                    "Rx:" + movingTo[3] + " " +
                    "Ry:" + movingTo[4] + " " +
                    "Rz:" + movingTo[5];
            }
            
        }

        //private async void btnGetPosVar_Click(object sender, EventArgs e)
        //{
        //    CRobPosVar posVar = new CRobPosVar();
        //    await xrc.ReadPositionVariable(varIndex, posVar);
        //    UpdateUiPositionVariable(posVar);
        //}

        private void readByteVariableExample()
        {
            double[] doubles = new double[10];
            xrc.ReadByteVariable(0, doubles);
            WriteLine("    Byte var: " + doubles[0].ToString());
        }

        //private async void button1_Click(object sender, EventArgs e)
        //{
        //    await xrc.MoveByJob(false, posSP);
        //}

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            speedSP = numericUpDown1.Value;
        }

        private bool isOperating() { return xrc.isOperating(); }
        private bool isNotOperating() { return !xrc.isOperating(); }

        private async void button2_Click(object sender, EventArgs e)
        {
            //button2.Enabled = false;
            //isCycling = !isCycling;
            //if (isCycling) button2.Text = "Stop Cycle";
            //else button2.Text = "Start Cycle";

            //if (!isCycling) await xrc.CancelOperation();

            //button2.Enabled = true;
            //while (isCycling)
            //{
            //    double[] posA = { -600, -1500, 200, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //    await xrc.MoveIncrementally(posA, (double)speedSP);
            //    await TaskEx.WaitUntil(isOperating);
            //    await TaskEx.WaitUntil(isNotOperating);
            //    await Task.Delay(200);

            //    await xrc.MoveToHome1();
            //    await TaskEx.WaitUntil(isOperating);
            //    await TaskEx.WaitUntil(isNotOperating);
            //    await Task.Delay(200);

            //    if (!isCycling) return;

            //    double[] posB = { 800, -1600, -400, 0, 85, 0, 0, 0, 0, 0, 0, 0 };
            //    await xrc.MoveIncrementally(posB, (double)speedSP);
            //    await TaskEx.WaitUntil(isOperating);
            //    await TaskEx.WaitUntil(isNotOperating);
            //    await Task.Delay(200);

            //    await xrc.MoveToHome1();
            //    await TaskEx.WaitUntil(isOperating);
            //    await TaskEx.WaitUntil(isNotOperating);
            //    await Task.Delay(200);
            //}

        }

        private void button4_Click(object sender, EventArgs e)
        {
            // UP
            ShiftRobit(new IMove(0, 0, -200, 0));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // DOWN
            ShiftRobit(new IMove(0, 0, 200, 0));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // RIGHT
            ShiftRobit(new IMove(0, 200, 0, 0));
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // LEFT
            ShiftRobit(new IMove(0, -200, 0, 0));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // BACK
            ShiftRobit(new IMove(-200, 0, 0, 0));
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // FORWARD
            ShiftRobit(new IMove(200, 0, 0, 0));
        }

        private void ShiftRobit(IMove iMove)
        {
            if (xrc == null) return;
            xrc.MoveIncrementally(iMove, (double)speedSP);
        }

        private async void btnUp_Click(object sender, EventArgs e)
        {
            // Position A
            if (xrc == null) return;

            IMove iMove = new IMove(200, 600, 1200, 0);
            await xrc.MoveIncrementally(iMove, (double)speedSP);
        }

        private async void btnDown_Click(object sender, EventArgs e)
        {
            // Position B
            if (xrc == null) return;

            IMove iMove = new IMove(-400, -600, 1200, -85);
            await xrc.MoveIncrementally(iMove, (double)speedSP);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            StartApp();
            button7.Enabled = true;
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            if (xrc == null) return;
            button7.Enabled = false;
            await xrc.Disconnect();
            xrc = null;
            btnConnect.Enabled = true;
        }
    }
}

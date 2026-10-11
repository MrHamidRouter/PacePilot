// PacePilot Sensor Mapping: choose exact real LHM sensor IDs, persisted by AppSettings.
// Unknown and temporarily absent sensors remain N/A. No heuristic guessing.
using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PacePilot
{
    public sealed class SensorMappingForm : Form
    {
        readonly ComboBox gpuBox=new ComboBox();
        readonly ComboBox boardBox=new ComboBox();
        readonly Label status=new Label();
        readonly bool persian;
        readonly string currentGpu,currentBoard;
        public string SelectedGpuId {get;private set;}
        public string SelectedBoardId {get;private set;}
        string L(string en,string fa){return persian?fa:en;}

        sealed class Choice
        {
            public string Id;
            public string Label;
            public override string ToString(){return Label;}
        }
        public SensorMappingForm(AppSettings settings,bool fa)
        {
            persian=fa;
            currentGpu=settings.GpuVrmSensorId??"";
            currentBoard=settings.BoardVrmSensorId??"";
            SelectedGpuId=currentGpu;
            SelectedBoardId=currentBoard;
            Text=L("Manual VRM Sensor Mapping","انتخاب دستی سنسور VRM");
            ClientSize=new Size(720,405);
            MinimumSize=new Size(640,400);
            StartPosition=FormStartPosition.CenterParent;
            FormBorderStyle=FormBorderStyle.FixedDialog;
            MaximizeBox=false;MinimizeBox=false;
            BackColor=Color.FromArgb(15,23,32);
            ForeColor=Color.White;
            Font=new Font("Segoe UI",9);
            var title=Label(L("Select the real temperature sensor (identifier is saved)",
                "سنسور دمای واقعی را انتخاب کنید (شناسه ذخیره می‌شود)"),18,14,680,35,13,true);
            var warning=Label(L("An unverified sensor must not be labeled VRM. Confirm the identity first.",
                "حسگر ناشناخته را بدون تأیید هویت، VRM نام‌گذاری نکنید."),18,50,681,46,9,false);
            warning.ForeColor=Color.Gold;
            Label(L("GPU VRM temperature sensor","سنسور دمای VRM گرافیک"),18,104,680,23,10,true);
            Setup(gpuBox,18,130);
            Label(L("Motherboard VRM temperature sensor","سنسور دمای VRM مادربرد"),18,181,680,23,10,true);
            Setup(boardBox,18,208);
            status.SetBounds(18,253,680,60);status.ForeColor=Color.FromArgb(173,191,208);
            Controls.Add(status);
            Button refresh=Button(L("Refresh sensor list","بروزرسانی سنسورها"),18,335,180);
            refresh.Click+=(sender,e)=>
            {
                SystemSample dummy=new SystemSample();
                BuiltInSensors.Enrich(ref dummy);
                status.Text=L("Refreshing cached hardware samples. Reopen if a sensor was just connected.",
                    "در حال بررسی حسگرها؛ اگر تازه شناسایی شده‌اند چند ثانیه بعد دوباره بزنید.");
                var once=new System.Windows.Forms.Timer();
                once.Interval=1200;
                once.Tick+=(s,t)=>{once.Stop();once.Dispose();if(!IsDisposed)Populate(
                    ChoiceId(gpuBox),ChoiceId(boardBox));};
                once.Start();
            };
            Button save=Button(L("Save mapping","ذخیره انتخاب"),502,335,196);
            save.BackColor=Color.FromArgb(27,181,160);
            save.ForeColor=Color.Black;
            save.Click+=(sender,e)=>
            {
                SelectedGpuId=ChoiceId(gpuBox);
                SelectedBoardId=ChoiceId(boardBox);
                DialogResult=DialogResult.OK;
                Close();
            };
            Populate(currentGpu,currentBoard);
        }
        Label Label(string text,int x,int y,int w,int h,int size,bool bold)
        {
            Label l=new Label();
            l.Text=text;l.SetBounds(x,y,w,h);l.Font=new Font("Segoe UI",size,
                bold?FontStyle.Bold:FontStyle.Regular);
            l.ForeColor=Color.White;l.RightToLeft=persian?RightToLeft.Yes:RightToLeft.No;
            l.TextAlign=persian?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft;
            Controls.Add(l);return l;
        }
        Button Button(string text,int x,int y,int w)
        {
            Button button=new Button();button.Text=text;button.SetBounds(x,y,w,40);
            button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(32,47,62);
            button.ForeColor=Color.White;Controls.Add(button);return button;
        }
        void Setup(ComboBox box,int x,int y)
        {
            box.SetBounds(x,y,680,32);
            box.DropDownStyle=ComboBoxStyle.DropDownList;
            box.BackColor=Color.FromArgb(29,42,55);
            box.ForeColor=Color.White;
            box.DropDownWidth=850;
            Controls.Add(box);
        }
        static string ChoiceId(ComboBox box)
        {
            Choice item=box.SelectedItem as Choice;
            return item==null?"":item.Id;
        }
        void Fill(ComboBox box,bool gpu,string selected)
        {
            box.BeginUpdate();
            try
            {
                box.Items.Clear();
                box.Items.Add(new Choice{Id="",Label=L("Automatic (no manual mapping)","خودکار (بدون انتخاب دستی)")});
                var candidates=HardwareSensorService.ReadLatest()
                    .Where(x=>gpu?HardwareSensorService.IsGpu(x):HardwareSensorService.IsMotherboard(x))
                    .OrderBy(x=>x.HardwarePath).ThenBy(x=>x.Name).ToArray();
                foreach(HardwareSensorRecord sensor in candidates)
                    box.Items.Add(new Choice{Id=sensor.Id,Label=sensor.ToString()});
                if(!String.IsNullOrEmpty(selected)&&!candidates.Any(x=>String.Equals(x.Id,selected,StringComparison.OrdinalIgnoreCase)))
                    box.Items.Add(new Choice{Id=selected,Label=L("Previously mapped sensor (currently missing): ",
                        "سنسور ذخیره‌شده (فعلاً پیدا نشد): ")+selected});
                box.SelectedIndex=0;
                for(int i=0;i<box.Items.Count;i++)
                {
                    Choice c=box.Items[i] as Choice;
                    if(c!=null&&String.Equals(c.Id,selected,StringComparison.OrdinalIgnoreCase))
                    {box.SelectedIndex=i;break;}
                }
            }
            finally{box.EndUpdate();}
        }
        void Populate(string gpu,string board)
        {
            Fill(gpuBox,true,gpu);Fill(boardBox,false,board);
            status.Text=L("Sensor engine: ","موتور حسگر: ")+BuiltInSensors.Status+"\n"+
                L("Temperature sensors: ","تعداد سنسورهای دما: ")+HardwareSensorService.ReadLatest().Count+
                L("    |  Missing values are shown as N/A.","  |  داده ناموجود به‌صورت N/A نمایش داده می‌شود.");
        }
    }
}

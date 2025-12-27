using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NativeInvoker;

namespace WindowManipulator
{
    public partial class MainForm : Form
    {
        public static MainForm self;
        public List<WindowControl> windows = new List<WindowControl>();
        public WindowControl selectedWindow;

        public string[] windowStyleNames;

        bool updating;

        public MainForm()
        {
            self = this;
            InitializeComponent();

            ScreenArea.AddBorder();
            InspectorPanel.AddBorder();

            var enumNames = Enum.GetNames(typeof(NativeWindowState));
            foreach (var name in enumNames)
                I_StateDropdown.Items.Add(name);
            I_StateDropdown.SelectedIndex = 5;

            windowStyleNames = Enum.GetNames(typeof(NativeWindowStyle));
            foreach (var name in windowStyleNames)
                I_StyleCheckBoxList.Items.Add(name);
        }

        private void findProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FindProcessDialog(this);
            dialog.Show();
        }

        public void AddWindow(NativeWindow window, ProcessInfo info, NativeWindowState state = NativeWindowState.ShowNA)
        {
            var control = new WindowControl();
            ScreenArea.Controls.Add(control);
            control.AssignInfo(window, info);
            windows.Add(control);
            RequestSelection(control);
        }

        public void UnregisterWindow(WindowControl control)
        {
            if(control == selectedWindow)
            {
                UpdateEmpty();
                selectedWindow = null;
            }
            windows.Remove(control);
        }

        public void RequestSelection(WindowControl control)
        {
            if (control == selectedWindow) return;
            foreach(var window in windows)
                window.SetSelected(control == window);
            OnSelect(control);
        }

        public void OnSelect(WindowControl control)
        {
            selectedWindow = control;
            UpdateIfSelected(selectedWindow);
        }

        public void UpdateIfSelected(WindowControl control)
        {
            if (control != selectedWindow) return;

            updating = true;
            I_TitleBox.Text = control.info.ProcessWindowTitle;
            I_ProcessNameBox.Text = control.info.Process.ProcessName;
            I_ProcessIDBox.Text = control.info.Process.Id.ToString();
            I_SizeBoxX.Text = control.window.size.x.ToString("0");
            I_SizeBoxY.Text = control.window.size.y.ToString("0");
            I_PositionBoxX.Text = control.window.position.x.ToString("0");
            I_PositionBoxY.Text = control.window.position.y.ToString("0");
            I_StateDropdown.SelectedIndex = (int)control.window.windowState;

            var style = control.window.windowStyle;
            Console.WriteLine(style);
            for (int i = 0; i < windowStyleNames.Length; i++)
            {
                Enum parsed = (NativeWindowStyle)Enum.Parse(typeof(NativeWindowStyle), windowStyleNames[i]);
                I_StyleCheckBoxList.SetItemChecked(i, style.HasFlag(parsed));
            }
            updating = false;
        }

        public static IEnumerable<Enum> GetFlags(Enum e)
        {
            return Enum.GetValues(e.GetType()).Cast<Enum>().Where(e.HasFlag);
        }

        public void UpdateEmpty()
        {
            updating = true;
            I_TitleBox.Text = null;
            I_ProcessNameBox.Text = null;
            I_ProcessIDBox.Text = null;
            I_SizeBoxX.Text = "0";
            I_SizeBoxY.Text = "0";
            I_PositionBoxX.Text = "0";
            I_PositionBoxY.Text = "0";
            I_StateDropdown.SelectedIndex = 5;
            updating = false;
        }

        public int screenIndex;
        public Vector2 ratio => ScreenArea.Size / GetSizeFromRect(Screen.PrimaryScreen.Bounds);

        public static Vector2 GetSizeFromRect(Rectangle rect) => new Vector2(rect.Width, rect.Height);
        public Vector2 GetWindowSize(NativeWindow win) => win.size * ratio;
        public Vector2 GetWindowPosition(NativeWindow win) => win.position * ratio;
        public Vector2 ReverseWindowSize(Control winPanel) => winPanel.Size / ratio;
        public Vector2 ReverseWindowPosition(Control winPanel) => winPanel.Location / ratio;
        public void SetWindowIndex(WindowControl control, int index) => ScreenArea.Controls.SetChildIndex(control, index);
        public int GetWindowsCount => ScreenArea.Controls.Count;

        private void I_ApplyStateButton_Click(object sender, EventArgs e)
        {
            if (updating) return;
            if (selectedWindow != null)
            {
                selectedWindow.window.windowState = (NativeWindowState)I_StateDropdown.SelectedIndex;
                selectedWindow.RefreshTransform();
            }
        }

        private void I_PositionBoxX_TextChanged(object sender, EventArgs e)
        {
            if (updating) return;
            if(selectedWindow != null)
            {
                var vec = selectedWindow.window.position;
                float.TryParse(I_PositionBoxX.Text, out vec.x);
                selectedWindow.window.position = vec;
                selectedWindow.RefreshTransform();
            }
        }

        private void I_PositionBoxY_TextChanged(object sender, EventArgs e)
        {
            if (updating) return;
            if (selectedWindow != null)
            {
                var vec = selectedWindow.window.position;
                float.TryParse(I_PositionBoxY.Text, out vec.y);
                selectedWindow.window.position = vec;
                selectedWindow.RefreshTransform();
            }
        }

        private void I_SizeBoxX_TextChanged(object sender, EventArgs e)
        {
            if (updating) return;
            if (selectedWindow != null)
            {
                var vec = selectedWindow.window.size;
                float.TryParse(I_SizeBoxX.Text, out vec.x);
                selectedWindow.window.size = vec;
                selectedWindow.RefreshTransform();
            }
        }

        private void I_SizeBoxY_TextChanged(object sender, EventArgs e)
        {
            if (updating) return;
            if (selectedWindow != null)
            {
                var vec = selectedWindow.window.size;
                float.TryParse(I_SizeBoxY.Text, out vec.y);
                selectedWindow.window.size = vec;
                selectedWindow.RefreshTransform();
            }
        }

        private void I_ApplyStyleButton_Click(object sender, EventArgs e)
        {
            if (updating) return;
            if (selectedWindow != null)
            {
                NativeWindowStyle style = default;
                for (int i = 0; i < I_StyleCheckBoxList.Items.Count; i++)
                {
                    if (I_StyleCheckBoxList.GetItemChecked(i))
                    {
                        string str = (string)I_StyleCheckBoxList.Items[i];
                        NativeWindowStyle newStyle;
                        if (Enum.TryParse(str, out newStyle))
                            style = style | newStyle;
                    }
                }
                selectedWindow.window.windowStyle = style;
                selectedWindow.RefreshTransform();
            }
        }

        private void I_KillProcessButton_Click(object sender, EventArgs e)
        {
            if (updating) return;
            if (selectedWindow != null)
            {
                selectedWindow.info.Process.Kill();
                selectedWindow.CloseWindow();
            }
        }

        private void I_CloseProcessButton_Click(object sender, EventArgs e)
        {
            if (updating) return;
            if (selectedWindow != null)
            {
                selectedWindow.info.Process.CloseMainWindow();
                selectedWindow.CloseWindow();
            }
        }

        private void documentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The developer doesn't yet have any idea of what this program does so this is still coming soon!", "Info");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutForm().Show();
        }
    }
}

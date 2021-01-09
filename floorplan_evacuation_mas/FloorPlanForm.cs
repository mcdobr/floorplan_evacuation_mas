using System;
using System.Drawing;
using System.Windows.Forms;
using floorplan_evacuation_mas;

namespace Reactive
{
    public partial class FloorPlanForm : Form
    {
        private MonitorAgent ownerAgent;
        private Bitmap doubleBufferImage;

        public FloorPlanForm()
        {
            InitializeComponent();
        }

        public void SetOwner(MonitorAgent a)
        {
            ownerAgent = a;
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawPlanet();
        }

        public void UpdatePlanetGUI()
        {
            DrawPlanet();
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            DrawPlanet();
        }

        private void DrawPlanet()
        {
            int w = pictureBox.Width;
            int h = pictureBox.Height;

            if (doubleBufferImage != null)
            {
                doubleBufferImage.Dispose();
                GC.Collect(); // prevents memory leaks
            }

            doubleBufferImage = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(doubleBufferImage);
            g.Clear(Color.White);

            int minXY = Math.Min(w, h);
            int cellSize = (minXY - 40) / Utils.Size;

            for (int i = 0; i <= Utils.Size; i++)
            {
                g.DrawLine(Pens.DarkGray, 20, 20 + i * cellSize, 20 + Utils.Size * cellSize, 20 + i * cellSize);
                g.DrawLine(Pens.DarkGray, 20 + i * cellSize, 20, 20 + i * cellSize, 20 + Utils.Size * cellSize);
            }

            if (ownerAgent != null)
            {
                foreach (Tuple<int, int> workerPosition in ownerAgent.WorkerPositions.Values)
                {
                    g.FillEllipse(Brushes.Blue,
                        20 + workerPosition.Item1 * cellSize + 6,
                        20 + workerPosition.Item2 * cellSize + 6,
                        cellSize - 12,
                        cellSize - 12
                    );
                }

                foreach (Tuple<int, int> exitPosition in ownerAgent.ExitPositions.Values)
                {
                    g.FillRectangle(Brushes.LightGreen,
                        20 + exitPosition.Item1 * cellSize + 10,
                        20 + exitPosition.Item2 * cellSize + 10,
                        cellSize - 20,
                        cellSize - 20
                    );
                }
            }

            Graphics pbg = pictureBox.CreateGraphics();
            pbg.DrawImage(doubleBufferImage, 0, 0);
        }
    }
}
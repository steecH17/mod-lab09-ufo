using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text;
using System.Linq;
using ScottPlot;

namespace UfoMovement
{
    public partial class MainForm : Form
    {
        private readonly Point startPoint = new Point(100, 100);
        private readonly Point endPoint = new Point(1500, 800);

        private Point currentPoint;
        private double angle;
        private int step = 5;
        private int precision = 10;
        private System.Windows.Forms.Timer timer;
        private Bitmap canvas;
        private Graphics g;
        private bool isDrawing = false;
        private bool lineDrawn = false;
        private int targetRadius = 20;

        public MainForm()
        {
            InitializeComponent();
            AddControlsOnForm();
            SetInitSettings();
        }

        private void SetInitSettings()
        {
            this.DoubleBuffered = true;
            this.ClientSize = new Size(900, 600);
            this.Text = "UFO Movement Simulation";

            canvas = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            g = Graphics.FromImage(canvas);
            g.ScaleTransform(0.5f, 0.5f);

            currentPoint = startPoint;
            CalculateAngle();

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 50;
            timer.Tick += Timer_Tick;
        }

        private void AddControlsOnForm()
        {
            var drawButton = new Button
            {
                Text = "Draw",
                Size = new Size(120, 40),
                Location = new Point(this.ClientSize.Width - 140, 20)
            };
            drawButton.Click += DrawButton_Click;

            var statsButton = new Button
            {
                Text = "Run Statistics",
                Size = new Size(120, 40),
                Location = new Point(this.ClientSize.Width - 140, 80)
            };
            statsButton.Click += StatsButton_Click;

            var radiusLabel = new System.Windows.Forms.Label
            {
                Text = "Target Radius:",
                Location = new Point(this.ClientSize.Width - 140, 140),
                AutoSize = true
            };

            var radiusNumeric = new NumericUpDown
            {
                Minimum = 2,
                Maximum = 100,
                Value = targetRadius,
                Size = new Size(120, 40),
                Location = new Point(this.ClientSize.Width - 140, 160)
            };
            radiusNumeric.ValueChanged += (s, e) =>
            {
                targetRadius = (int)radiusNumeric.Value;
                this.Invalidate();
            };

            var precisionLabel = new System.Windows.Forms.Label
            {
                Text = "Precision (n):",
                Location = new Point(this.ClientSize.Width - 140, 220),
                AutoSize = true
            };

            var precisionNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 20,
                Value = precision,
                Size = new Size(120, 40),
                Location = new Point(this.ClientSize.Width - 140, 240)
            };
            precisionNumeric.ValueChanged += (s, e) =>
            {
                precision = (int)precisionNumeric.Value;
            };

            this.Controls.AddRange(new Control[] {
                drawButton,
                statsButton,
                radiusLabel,
                radiusNumeric,
                precisionLabel,
                precisionNumeric
            });
        }
        private void DrawButton_Click(object sender, EventArgs e)
        {
            if (!isDrawing)
            {
                isDrawing = true;
                lineDrawn = false;
                currentPoint = startPoint;
                CalculateAngle();
                timer.Start();
            }
        }

        private void StatsButton_Click(object sender, EventArgs e)
        {
            var radiuses = Enumerable.Range(1, 10).Select(x => x * 2).ToArray();
            var accuracies = radiuses.Select(r => FindMinPrecisionForRadius(r)).ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("Radius,MinPrecision");
            for (int i = 0; i < radiuses.Length; i++)
            {
                sb.AppendLine($"{radiuses[i]},{accuracies[i]}");
            }
            File.WriteAllText("../result/data.txt", sb.ToString());

            string plotPath = Path.Combine("../result", "plot.png");
            CreatePlot(radiuses, accuracies, plotPath);

            MessageBox.Show("Statistics collected and plot generated!");
        }

        private void CreatePlot(int[] radiuses, int[] accuracies, string plotPath)
        {
            var plot = new Plot();
            plot.Title("Зависимость точности от радиуса попадания", size: 16);
            plot.XLabel("Радиус попадания");
            plot.YLabel("Минимальное количество членов ряда (n)");

            var scatter = plot.Add.Scatter(radiuses.Select(x => (double)x).ToArray(),
                                         accuracies.Select(x => (double)x).ToArray());
            scatter.Color = ScottPlot.Colors.Blue;
            scatter.MarkerSize = 7;
            scatter.LineWidth = 2;

            plot.SavePng(plotPath, 800, 600);
        }

        private int FindMinPrecisionForRadius(int radius)
        {
            for (int n = 1; n <= 20; n++)
            {
                double x = startPoint.X;
                double y = startPoint.Y;
                bool reached = false;

                for (int i = 0; i < 2000; i++)
                {
                    double dx = step * CustomCos(angle, n);
                    double dy = step * CustomSin(angle, n);

                    x += dx;
                    y += dy;

                    double distance = Math.Sqrt(Math.Pow(endPoint.X - x, 2) + Math.Pow(endPoint.Y - y, 2));
                    if (distance <= radius)
                    {
                        reached = true;
                        break;
                    }

                    if ((step > 0 && x > endPoint.X) || (step < 0 && x < endPoint.X))
                        break;
                }

                if (reached) return n;
            }
            return 20;
        }

        private void CalculateAngle()
        {
            int dx = endPoint.X - startPoint.X;
            int dy = endPoint.Y - startPoint.Y;
            angle = Math.Atan2(dy, dx);
        }

        private double CustomSin(double x, int n)
        {
            x = x % (2 * Math.PI);
            double result = 0;
            double term = x;
            int power = 1;

            for (int i = 0; i < n; i++)
            {
                result += term;
                power += 2;
                term = -term * x * x / (power * (power - 1));
            }
            return result;
        }

        private double CustomCos(double x, int n)
        {
            x = x % (2 * Math.PI);
            double result = 0;
            double term = 1;
            int power = 0;

            for (int i = 0; i < n; i++)
            {
                result += term;
                power += 2;
                term = -term * x * x / (power * (power - 1));
            }
            return result;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            double dx = step * CustomCos(angle, precision);
            double dy = step * CustomSin(angle, precision);

            currentPoint.X += (int)dx;
            currentPoint.Y += (int)dy;

            double distance = Math.Sqrt(Math.Pow(endPoint.X - currentPoint.X, 2) +
                            Math.Pow(endPoint.Y - currentPoint.Y, 2));

            if (distance <= targetRadius ||
                (step > 0 && currentPoint.X > endPoint.X) ||
                (step < 0 && currentPoint.X < endPoint.X))
            {
                timer.Stop();
                isDrawing = false;
                lineDrawn = true;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            g.Clear(this.BackColor);

            g.FillEllipse(Brushes.Green, startPoint.X - 10, startPoint.Y - 10, 20, 20);
            g.FillEllipse(Brushes.Red, endPoint.X - 5, endPoint.Y - 5, 10, 10);
            g.DrawEllipse(Pens.Red, endPoint.X - targetRadius, endPoint.Y - targetRadius,
                        targetRadius * 2, targetRadius * 2);

            g.DrawLine(Pens.Gray, startPoint, endPoint);

            if (lineDrawn || isDrawing)
            {
                g.FillEllipse(Brushes.Blue, currentPoint.X - 5, currentPoint.Y - 5, 10, 10);
                g.DrawLine(Pens.Blue, startPoint, currentPoint);
            }

            e.Graphics.DrawImage(canvas, 0, 0);
        }
    }
}